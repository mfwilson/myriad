namespace Myriad

open System
open System.Collections.Concurrent
open System.Runtime.InteropServices

type Dimension = 
    { Id : Int32; Name : String }
    override x.ToString() = String.Concat("Dimension [", x.Name, "] [", x.Id, "]")

type Property = 
    { Id : Int32; Name : String }
    override x.ToString() = String.Concat("Property [", x.Name, "] [", x.Id, "]")

/// type DimensionSet 
/// map Int32 -> Dimension
/// map String -> Dimension
/// ds 

/// Measures are equivalent over dimension id and value
[<CustomEquality;CustomComparison>]
type Measure = 
    struct
        val DimensionId : Int32
        val DimensionName : String
        val Value : String 
        new(dimensionId : Int32, dimensionName : String, value : String) = 
            { DimensionId = dimensionId; DimensionName = dimensionName; Value = value}
        new(dimension : Dimension, value : String) = 
            { DimensionId = dimension.Id; DimensionName = dimension.Name; Value = value}
    end

    override x.ToString() = String.Format("'{0}' [{1}] = '{2}'", x.DimensionName, x.DimensionId, x.Value)

    override x.Equals(obj) = 
        match obj with
        | :? Measure as y -> Measure.CompareTo(x, y) = 0
        | _ -> false

    override x.GetHashCode() = hash(x.DimensionId, x.Value)
    
    static member CompareTo(x : Measure, y : Measure) = compare (x.DimensionId, x.Value) (y.DimensionId, y.Value)

    interface IComparable with
        member x.CompareTo other = 
            match other with 
            | :? Measure as y -> Measure.CompareTo(x, y)
            | _ -> invalidArg "other" "cannot compare value of different types" 

/// Clusters are equivalent over their measures set
[<CustomEquality;CustomComparison>]
type Cluster = 
    struct
        val Id : Int32
        val Timestamp : Int64
        val Property : Property
        val Value : String
        val Measures : Set<Measure> 
        new(id : Int32, timestamp : Int64, property : Property, value : String, measures : Set<Measure>) =
            { Id = id; Timestamp = timestamp; Property = property; Value = value; Measures = measures }
    end

    override x.Equals(obj) = 
        match obj with
        | :? Cluster as y -> (x.Measures = y.Measures)
        | _ -> false

    override x.GetHashCode() = hash(x.Measures)
    
    static member CompareTo(x : Set<Measure>, y : Set<Measure>) = compare x y

    interface IComparable with
        member x.CompareTo other = 
            match other with 
            | :? Cluster as y -> Cluster.CompareTo(x.Measures, y.Measures)
            | _ -> invalidArg "other" "cannot compare value of different types" 


type Context = { AsOf : DateTimeOffset; Measures : Set<Measure> }

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


/// QueryCache
/// QueryBuilder

// string (key) -> Int64 (time) list -> sorted Cluster list 
// 
// ConcurrentDictionary<string, lockfreelist< list<cluster> > >
//                      hash,    binary search, binary search
type DimensionCache(dimensions : Dimension seq, collection : Cluster seq) = 
    
    let cache = new ConcurrentDictionary<String, LockFreeList<TimestampList>>()
    
    // Dimension Id -> weight
    let weights = dimensions |> Seq.mapi (fun i d -> int d.Id, int (2.0 ** float i)) |> Map.ofSeq
        
    let compareMeasures (x : Cluster) (y : Cluster) = 
        let xWeight = x.Measures |> Seq.sumBy (fun m -> weights.[m.DimensionId])  
        let yWeight = y.Measures |> Seq.sumBy (fun m -> weights.[m.DimensionId])  
        yWeight.CompareTo(xWeight)

    /// If the cluster's measures are a subset of the context's measures, then it is a match
    let matchMeasures (context : Context) (cluster : Cluster) = Set.isSubset (cluster.Measures) (context.Measures)

    let addOrUpdate(key : String, clusters : Cluster seq) =        
        let clustersByWeight = clusters |> Seq.sortWith compareMeasures |> Seq.toList
        let lastCluster = clusters |> Seq.maxBy (fun c -> c.Timestamp) 
        let timestampList = { Timestamp = lastCluster.Timestamp; Clusters = clustersByWeight }        
        cache.[key] <- new LockFreeList<TimestampList>( [ timestampList ] )

    do
        //collection |> Seq.sortWith compareClusters |> Seq.iter addOrUpdate
        collection |> Seq.groupBy (fun c -> c.Property.Name) |> Seq.iter addOrUpdate

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
                let cluster = instance.Value.Clusters |> List.tryFind (fun c -> matchMeasures context c)
                if cluster.IsNone then 
                    false 
                else
                    value <- cluster.Value.Value
                    true
                