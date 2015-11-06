namespace Myriad

open System
open System.Collections.Concurrent
open System.Runtime.InteropServices

type MyriadCache() =
    let cache = new ConcurrentDictionary<String, LockFreeList<ClusterSet>>()

    let isSubset (first : Set<Measure>) (second : Set<Measure>) = 
        let g = Set.isSubset first second
        g
            
    let getClusterSetByTime (ticks) (clusterSets : ClusterSet list) =
        clusterSets |> List.tryFind (fun tlist -> tlist.Timestamp <= ticks)

    /// If the cluster's measures are a subset of the context's measures, then it is a match
    let getClusterByContext (context) (clusters : Cluster list) =
        clusters |> List.tryFind (fun cluster -> isSubset (cluster.Measures) (context.Measures))

    /// If the context's measures are a subset of the cluster's measures, then it is a match
    let getAnyClusterByContext (context) (clusters : Cluster list) =
        clusters |> List.filter (fun cluster -> isSubset (context.Measures) (cluster.Measures)) 

    let getMatch (clusterSets : LockFreeList<ClusterSet> seq) (context : Context) =
        clusterSets
        |> Seq.choose (fun c -> getClusterSetByTime context.AsOf.UtcTicks c.Value)
        |> Seq.choose (fun c -> getClusterByContext context c.Clusters)

    let getAny (clusterSets : LockFreeList<ClusterSet> seq) (context : Context) =
        clusterSets
        |> Seq.choose (fun c -> getClusterSetByTime context.AsOf.UtcTicks c.Value)
        |> Seq.map (fun c -> getAnyClusterByContext context c.Clusters)
        |> Seq.concat
        
    let tryHead (ls:seq<'a>) : option<'a>  = ls |> Seq.tryPick Some

    member x.Keys with get() = cache.Keys

    member x.TryGetValue(key : String, context : Context, [<Out>] value : byref<String>) =
        let success, property = x.TryFind(key, context)
        if not success || property.IsNone then 
            false 
        else 
            value <- property.Value.Value
            true

    /// Find the cluster that best matches the property and context
    member x.TryFind(key : String, context : Context, [<Out>] value : byref<Cluster option>) =
        let success, result = cache.TryGetValue key
        if not success then
            value <- None
            false
        else
            // Find 1st item less then timestamp
            value <- [ result.Value ]
                     |> Seq.choose (fun c -> getClusterSetByTime context.AsOf.UtcTicks c)
                     |> Seq.choose (fun c -> getClusterByContext context c.Clusters)
                     |> tryHead
            value.IsSome

    member x.GetProperties(context : Context) = getMatch cache.Values context

    member x.GetAny(context : Context) = getAny cache.Values context

    member x.GetAny(key : String, context : Context) =
        let success, result = cache.TryGetValue key
        if not success then Seq.empty else getAny [ result ] context

    member x.Insert(clusterSet : ClusterSet) =
        let add = 
            new Func<string, LockFreeList<ClusterSet>>(
                fun(key : string) -> new LockFreeList<ClusterSet>( [ clusterSet ] ) )

        let update = 
            new Func<string, LockFreeList<ClusterSet>, LockFreeList<ClusterSet>>(
                fun(key : string) (current : LockFreeList<ClusterSet>) -> current.Add clusterSet )

        cache.AddOrUpdate(clusterSet.Key, add, update) |> ignore
