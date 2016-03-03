namespace Myriad

open System
open System.Collections.Concurrent
open System.Runtime.InteropServices

type MyriadCache() =
    let cache = new ConcurrentDictionary<String, LockFreeList<Property>>(StringComparer.InvariantCultureIgnoreCase)

    let isSubset (first : Set<Measure>) (second : Set<Measure>) = Set.isSubset first second
            
    let getPropertyByTime (ticks) (properties : Property list) =
        properties |> List.tryFind (fun tlist -> tlist.Timestamp <= ticks)

    /// If the cluster's measures are a subset of the context's measures, then it is a match
    let getPropertyByContext (context : Context) (property : Property) =
        let result = property.Clusters |> List.tryFind (fun cluster -> isSubset (cluster.Measures) (context.Measures))
        if result.IsNone then None else Some(property, result.Value)

    /// If the context's measures are a subset of the cluster's measures, then it is a match
    let getAnyPropertyByContext (context : Context) (property : Property) =
        property.Clusters 
        |> List.filter (fun cluster -> isSubset (context.Measures) (cluster.Measures)) 
        |> List.map (fun cluster -> property, cluster)
        
    let getMatch (properties : LockFreeList<Property> seq) (context : Context) =
        properties
        |> Seq.choose (fun p -> getPropertyByTime context.AsOf.UtcTicks p.Value)
        |> Seq.choose (fun p -> getPropertyByContext context p)

    let getAny (properties : LockFreeList<Property> seq) (context : Context) =
        let any = properties
                  |> Seq.choose (fun c -> getPropertyByTime context.AsOf.UtcTicks c.Value)
                  |> Seq.map (fun c -> getAnyPropertyByContext context c)
                  |> Seq.concat
        if Seq.isEmpty any then getMatch properties context else any
        
    let tryHead (ls:seq<'a>) : option<'a>  = ls |> Seq.tryPick Some

    member x.Keys with get() = cache.Keys

    member x.TryGetValue(key : String, context : Context, [<Out>] value : byref<String>) =
        let success, result = x.TryFind(key, context)
        if not success || result.IsNone then 
            false 
        else 
            value <- snd(result.Value).Value
            true

    /// Find the cluster that best matches the property and context
    member x.TryFind(key : String, context : Context, [<Out>] value : byref<(Property * Cluster) option>) =
        let success, result = cache.TryGetValue key
        if not success then
            value <- None
            false
        else
            // Find 1st item less then timestamp
            value <- [ result.Value ]
                     |> Seq.choose (fun c -> getPropertyByTime context.AsOf.UtcTicks c)
                     |> Seq.choose (fun c -> getPropertyByContext context c)
                     |> tryHead
            value.IsSome

    member x.GetMatches(context : Context) = getMatch cache.Values context

    member x.GetAny(context : Context) = getAny cache.Values context

    member x.GetAny(key : String, context : Context) =
        let success, result = cache.TryGetValue key
        if not success then Seq.empty else getAny [ result ] context

    member x.GetProperty(key : String, asOf : DateTimeOffset) =
        let success, result = cache.TryGetValue key
        if not success then
            None            
        else
            // Find 1st item less then timestamp
            [ result.Value ]
            |> Seq.choose (fun p -> getPropertyByTime asOf.UtcTicks p)
            |> tryHead

    member x.GetProperties() =         
        cache.Values |> Seq.choose (fun p -> if p.Value.IsEmpty then None else Some p.Value.Head)

    member x.Insert(property : Property) =
        let add = 
            new Func<string, LockFreeList<Property>>(
                fun(key : string) -> new LockFreeList<Property>( [ property ] ) )

        let update = 
            new Func<string, LockFreeList<Property>, LockFreeList<Property>>(
                fun(key : string) (current : LockFreeList<Property>) -> current.Add property )

        cache.AddOrUpdate(property.Key, add, update) |> ignore
