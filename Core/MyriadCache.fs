namespace Myriad

open System
open System.Collections.Concurrent
open System.Runtime.InteropServices

type MyriadCache(dimensions : Dimension seq, collection : Cluster seq) = 
    
    let cache = new ConcurrentDictionary<String, LockFreeList<ClusterSet>>()
    
    // Dimension Id -> weight
    let weights = dimensions |> Seq.mapi (fun i d -> d.Id, int (2.0 ** float i)) |> Map.ofSeq
        
    let compareMeasures (x : Cluster) (y : Cluster) = 
        let xWeight = x.Measures |> Seq.sumBy (fun m -> weights.[m.DimensionId])  
        let yWeight = y.Measures |> Seq.sumBy (fun m -> weights.[m.DimensionId])  
        yWeight.CompareTo(xWeight)

    /// If the cluster's measures are a subset of the context's measures, then it is a match
    let matchMeasures (context : Context) (cluster : Cluster) = Set.isSubset (cluster.Measures) (context.Measures)

    let addOrUpdate(key : String, clusters : Cluster seq) =        
        let clustersByWeight = clusters |> Seq.sortWith compareMeasures |> Set.ofSeq
        let lastCluster = clusters |> Seq.maxBy (fun c -> c.Audit.Timestamp) 
        let timestampList = new ClusterSet(lastCluster.Audit.Timestamp, clustersByWeight)
        cache.[key] <- new LockFreeList<ClusterSet>( [ timestampList ] )

    do
        //collection |> Seq.sortWith compareClusters |> Seq.iter addOrUpdate
        collection |> Seq.groupBy (fun c -> c.Property.Name) |> Seq.iter addOrUpdate

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
                let cluster = instance.Value.Clusters |> Set.toSeq |> Seq.tryFind (fun c -> matchMeasures context c)
                if cluster.IsNone then 
                    false 
                else
                    value <- cluster.Value.Value
                    true

    member x.Append(cluster : Cluster) =
        // Operation: add, remove, update
        let key = cluster.Property.Name

        let success, result = cache.TryGetValue key
        //if not success then
            
            

        ignore()
