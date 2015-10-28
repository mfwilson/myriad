namespace Myriad

open System
open System.Collections.Concurrent
open System.Runtime.InteropServices

type MyriadCache() =
    let cache = new ConcurrentDictionary<String, LockFreeList<ClusterSet>>()

    /// If the cluster's measures are a subset of the context's measures, then it is a match
    let matchMeasures (context : Context) (cluster : Cluster) = Set.isSubset (cluster.Measures) (context.Measures)
            
    let getClusterSetByTime (ticks) (clusterSets : ClusterSet list) =
        clusterSets |> List.tryFind (fun tlist -> tlist.Timestamp <= ticks)

    let getClusterByContext (context) (clusters : Cluster list) =
        clusters |> List.tryFind (fun c -> matchMeasures context c)

    let tryHead (ls:seq<'a>) : option<'a>  = ls |> Seq.tryPick Some

    member x.Keys with get() = cache.Keys

    member x.TryGetValue(key : String, context : Context, [<Out>] value : byref<String>) =
        let success, property = x.TryFind(key, context)
        if not success || property.IsNone then 
            false 
        else 
            value <- property.Value.Value
            true

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

    member x.GetProperties(context : Context) =
        cache.Values 
        |> Seq.choose (fun c -> getClusterSetByTime context.AsOf.UtcTicks c.Value)
        |> Seq.choose (fun c -> getClusterByContext context c.Clusters)

    member x.Insert(clusterSet : ClusterSet) =
        let add = 
            new Func<string, LockFreeList<ClusterSet>>(
                fun(key : string) -> new LockFreeList<ClusterSet>( [ clusterSet ] ) )

        let update = 
            new Func<string, LockFreeList<ClusterSet>, LockFreeList<ClusterSet>>(
                fun(key : string) (current : LockFreeList<ClusterSet>) -> current.Add clusterSet )

        cache.AddOrUpdate(clusterSet.Key, add, update) |> ignore
