namespace Myriad 

open System

/// Clusters are equivalent over their measures set
/// contain a value, set of measures, and a username
[<CustomEquality;CustomComparison>]
type Cluster = 
    struct
        val Id : Int64
        val Value : String
        val Measures : Set<Measure>
        val Timestamp : Int64
        val UserName : String
        new(id : Int64, value : String, measures : Set<Measure>, timestamp : Int64, userName : String) =
            { Id = id; Value = value; Measures = measures; Timestamp = timestamp; UserName = userName }
        new(id : Int64, value : String, measures : Set<Measure>, audit : IAudit) =
            { Id = id; Value = value; Measures = measures; Timestamp = audit.Timestamp; UserName = audit.UserName }
        new(id : Int64, value : String, measures : Set<Measure>) =
            { Id = id; Value = value; Measures = measures; Timestamp = Epoch.GetOffset(DateTimeOffset.UtcNow.Ticks); UserName = Audit.CurrentUser }
    end

    interface IComparable with
        member x.CompareTo other = 
            match other with 
            | :? Cluster as y -> Cluster.CompareTo(x.Measures, y.Measures)
            | _ -> invalidArg "other" "cannot compare value of different types" 

    interface IAudit with
        member x.Timestamp with get() = x.Timestamp
        member x.UserName with get() = x.UserName

    override x.Equals(obj) = 
        match obj with
        | :? Cluster as y -> (x.Measures = y.Measures)
        | _ -> false

    override x.GetHashCode() = hash(x.Measures)
    
    override x.ToString() = 
        let measures = String.Join(", ", x.Measures)
        String.Format("{0} = '{1}', Measures: {2}", x.Id, x.Value, measures)

    static member ToMap(propertyKey : String, cluster : Cluster, dimensions : IDimension seq, ordinal : int) =
        let values = [ "Id", cluster.Id.ToString(); "Property", propertyKey; "Value", cluster.Value; "Ordinal", ordinal.ToString() ]
        let measures = cluster.Measures |> Set.toList |> List.map (fun m -> m.Dimension.Name, m.Value)

        let filterByDimension(dimension : IDimension) =
            not(measures |> Seq.exists (fun m -> fst(m) = dimension.Name))

        let defaults = dimensions |> Seq.filter filterByDimension |> Seq.map (fun d -> d.Name, "") |> Seq.toList
        Map.ofList (List.concat [ values; measures; defaults ])

    static member CompareTo(x : Set<Measure>, y : Set<Measure>) = compare x y

