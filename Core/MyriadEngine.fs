namespace Myriad

open System 

type MyriadEngine(store : IMyriadStore) =

    let cache = new MyriadCache()

    let dimensions = store.GetDimensions()
    let dimensionMap = dimensions |> Seq.map (fun d -> d.Name, d) |> Map.ofSeq
    let mb = MeasureBuilder(dimensionMap)
    let pb = PropertyBuilder(dimensions)

    do
        store.Initialize()
        store.GetProperties(MyriadHistory.All()) |> Seq.iter cache.Insert

    member x.MeasureBuilder with get() = mb
    member x.PropertyBuilder with get() = pb

    member x.GetDimension(key : String) = store.GetDimension(key)

    member x.GetDimensions() = dimensions

    member x.GetMetadata() = store.GetMetadata()

    /// Query for any values not filtered by the context; if property key is empty, query over all keys
    member x.Query(propertyKey : String, context : Context) =
        match propertyKey with
        | key when String.IsNullOrEmpty(key) -> cache.GetAny(context)
        | key -> cache.GetAny(key, context)
    
    /// Get values that are the best match and not filtered by the context; if property key is empty, find over all keys
    member x.Get(propertyKey : String, context : Context) =
        let properties = match propertyKey with
                         | key when String.IsNullOrEmpty(key) -> cache.GetMatches(context)
                         | key -> 
                             let success, result = cache.TryFind(key, context)
                             if result.IsNone then Seq.empty else [ result.Value ] |> Seq.ofList
        properties |> Seq.map (fun pair -> { Name = fst(pair).Key; Value = snd(pair).Value}) |> Seq.toList

    member x.Get(propertyKey : String, asOf : DateTimeOffset) =
        cache.GetProperty(propertyKey, asOf)

    member x.Set(property : Property) = 
        
        // TODO: Write through to store then insert in to cache
        cache.Insert(property)
