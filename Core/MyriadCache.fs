namespace Myriad

open System
open System.Collections.Concurrent
open System.Runtime.InteropServices

type MyriadCache() =
    let cache = new ConcurrentDictionary<String, LockFreeList<ClusterSet>>()

    /// If the cluster's measures are a subset of the context's measures, then it is a match
    let matchMeasures (context : Context) (cluster : Cluster) = Set.isSubset (cluster.Measures) (context.Measures)
    
    member x.Keys with get() = cache.Keys

    member x.TryFind(key : String, context : Context, [<Out>] value : byref<String>) =
        value <- null

        let success, result = cache.TryGetValue key
        if not success then
            false
        else
            // Find 1st item less then timestamp
            let current = result.Value
            let instance = current |> List.tryFind (fun tlist -> tlist.Timestamp <= context.AsOf.UtcTicks)
            if instance.IsNone then
                false
            else
                // Find 1st matching context
                let cluster = instance.Value.Clusters |> Seq.tryFind (fun c -> matchMeasures context c)
                if cluster.IsNone then 
                    false 
                else
                    value <- cluster.Value.Value
                    true

    member x.Insert(clusterSet : ClusterSet) =
        let add = 
            new Func<string, LockFreeList<ClusterSet>>(
                fun(key : string) -> new LockFreeList<ClusterSet>( [ clusterSet ] ) )

        let update = 
            new Func<string, LockFreeList<ClusterSet>, LockFreeList<ClusterSet>>(
                fun(key : string) (current : LockFreeList<ClusterSet>) -> current.Add clusterSet )

        cache.AddOrUpdate(clusterSet.Key, add, update) |> ignore
