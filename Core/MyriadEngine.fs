namespace Myriad

open System 

type MyriadEngine(store : IMyriadStore) =

    let cache = new MyriadCache()

    do
        store.Initialize()
        store.GetClusterSets(MyriadHistory.All()) |> Seq.iter cache.Insert


    member x.GetDimension(key : String) = store.GetDimension(key)

    member x.GetDimensions() = store.GetDimensions()

    member x.GetMetadata() = store.GetMetadata()

    /// Query for any values not filtered by the context; if property key is empty, query over all keys
    member x.Query(propertyKey : String, context : Context) =
        match propertyKey with
        | key when String.IsNullOrEmpty(key) -> cache.GetAny(context)
        | key -> cache.GetAny(key, context)
    
    /// Get values that are the best match and not filtered by the context; if property key is empty, find over all keys
    member x.Get(propertyKey : String, context : Context) =
        match propertyKey with
        | key when String.IsNullOrEmpty(key) -> cache.GetMatches(context)
        | key -> 
            let success, result = cache.TryFind(key, context)
            if result.IsNone then Seq.empty else [ result.Value ] |> Seq.ofList
