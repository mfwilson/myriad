namespace Myriad 

open System

/// Clusters are equivalent over their measures set
/// contain a value, set of measures, and a username
[<CustomEquality;CustomComparison>]
type Cluster = 
    { Value : String; Measures : Set<Measure>; UserName : String; Timestamp : Int64 }

    interface IComparable with
        member x.CompareTo other = 
            match other with 
            | :? Cluster as y -> Cluster.CompareTo(x.Measures, y.Measures)
            | _ -> invalidArg "other" "cannot compare value of different types" 

    override x.Equals(obj) = 
        match obj with
        | :? Cluster as y -> (x.Measures = y.Measures)
        | _ -> false

    override x.GetHashCode() = hash(x.Measures)
    
    override x.ToString() = 
        let measures = String.Join(", ", x.Measures)
        String.Format("[{0}], Measures: {1}", x.Value, measures)

    static member ToMap(propertyKey : String, cluster : Cluster, dimensions : Dimension seq, ordinal : int) =
        let values = [ "Property", propertyKey; "Value", cluster.Value; "Ordinal", ordinal.ToString() ]
        let measures = cluster.Measures |> Set.toList |> List.map (fun m -> m.Dimension.Name, m.Value)

        let filterByDimension(dimension : Dimension) =
            not(measures |> Seq.exists (fun m -> fst(m) = dimension.Name))

        let defaults = dimensions |> Seq.filter filterByDimension |> Seq.map (fun d -> d.Name, "") |> Seq.toList
        Map.ofList (List.concat [ values; measures; defaults ])

    static member CompareTo(x : Set<Measure>, y : Set<Measure>) = compare x y

    static member Create(value : String, measures : Set<Measure>) =
        { Value = value; Measures = measures; UserName = Environment.UserName; Timestamp = Epoch.UtcNow }


type ClusterOperation = 
| Add of PropertyKey : String * Added : Cluster
| Update of PropertyKey : String * Previous : Cluster * Updated : Cluster
| Remove of PropertyKey : String * Removed : Cluster
