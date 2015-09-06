namespace Myriad

open System
open System.Collections.Concurrent
open System.Runtime.InteropServices

type Dimension = { Id : Int32; Name : String }

type Property = { Id : Int32; Name : String }

//type DimensionSet = { Name : String; Dimensions : Dimension array }

type Measure = { Id : Int32; Name : String; Value : String }

type Cluster = 
    { Id : Int32; 
      Timestamp : Int64; 
      Property : Property; 
      Value : String; 
      Measures : Measure seq }

type Context = { Measures : Measure seq }

[<CustomEquality;CustomComparison>]
type TimestampList = 
    { Timestamp : Int64; Clusters : Cluster list }
    
    override x.Equals(yobj) = 
        match yobj with
        | :? TimestampList as y -> (x.Timestamp = y.Timestamp)
        | _ -> false

    override x.GetHashCode() = hash(x)
    
    interface IComparable with
        member x.CompareTo other = 
            match other with 
            | :? TimestampList as y -> x.Timestamp.CompareTo(y.Timestamp)
            | _ -> invalidArg "other" "cannot compare value of different types" 

// string (key) -> Int64 (time) list -> sorted Cluster list 
// 
// ConcurrentDictionary<string, lockfreelist< list<cluster> > >
//                      hash,    binary search, binary search
type DimensionCache(dimensions : Dimension seq, collection : Cluster seq) = 
    
    let cache = new ConcurrentDictionary<String, LockFreeList<TimestampList>>()
    
    // Dimension Id -> weight
    let weights = dimensions |> Seq.mapi (fun i d -> int d.Id, int (2.0 ** float i)) |> Map.ofSeq
        
    let compareMeasures (x : Cluster) (y : Cluster) = 
        let xWeight = x.Measures |> Seq.sumBy (fun m -> weights.[m.Id])  
        let yWeight = y.Measures |> Seq.sumBy (fun m -> weights.[m.Id])  
        xWeight.CompareTo(yWeight)

    /// If all the context's measures match, then it is a match
    let matchMeasures (x : Context) (y : Cluster) = 
        let source = x.Measures |> Seq.map (fun m -> m.Id, m.Value) |> Map.ofSeq 
        let target = y.Measures |> Seq.map (fun m -> m.Id, m.Value) |> Map.ofSeq 
        
        false        

    let addOrUpdate(key : String, clusters : Cluster seq) =        
        let clustersByWeight = clusters |> Seq.sortWith compareMeasures |> Seq.toList
        let lastCluster = clusters |> Seq.maxBy (fun c -> c.Timestamp) 
        let timestampList = { Timestamp = lastCluster.Timestamp; Clusters = clustersByWeight }        
        cache.[key] <- new LockFreeList<TimestampList>( [ timestampList ] )

    do
        //collection |> Seq.sortWith compareClusters |> Seq.iter addOrUpdate
        collection |> Seq.groupBy (fun c -> c.Property.Name) |> Seq.iter addOrUpdate

    member x.TryFind(key : String, asOf : DateTimeOffset, context : Context, [<Out>] value : byref<String>) =
        value <- null

        let success, result = cache.TryGetValue key
        if not success then
            false
        else
            // Find 1st item less then timestamp
            let current = result.Value
            let instance = current |> List.tryFind (fun tlist -> tlist.Timestamp <= asOf.UtcTicks)
            if instance.IsNone then
                false
            else
                // Find 1st matching context
                let cluster = instance.Value.Clusters |> List.tryFind (fun c -> matchMeasures context c)
                if cluster.IsNone then 
                    false 
                else
                    value <- cluster.Value.Value
                    true
                