namespace Myriad 

open System

/// Clusters are equivalent over their measures set
[<CustomEquality;CustomComparison>]
type Cluster = 
    struct
        val Id : Int64
        val Key : String
        val Value : String
        val Measures : Set<Measure>
        val Timestamp : Int64
        val UserName : String
        new(id : Int64, key : String, value : String, measures : Set<Measure>, timestamp : Int64, userName : String) =
            { Id = id; Key = key; Value = value; Measures = measures; Timestamp = timestamp; UserName = userName }
        new(id : Int64, key : String, value : String, measures : Set<Measure>, audit : IAudit) =
            { Id = id; Key = key; Value = value; Measures = measures; Timestamp = audit.Timestamp; UserName = audit.UserName }
        new(id : Int64, key : String, value : String, measures : Set<Measure>) =
            { Id = id; Key = key; Value = value; Measures = measures; Timestamp = Epoch.GetOffset(DateTimeOffset.UtcNow.Ticks); UserName = Audit.CurrentUser }
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
        String.Format("'{0}' [{1}] = '{2}', Measures: {3}", x.Key, x.Id, x.Value, measures)

    static member CompareTo(x : Set<Measure>, y : Set<Measure>) = compare x y

[<CustomEquality;CustomComparison>]
type ClusterSet = 
    struct
        val Key : String
        val Timestamp : Int64
        val Clusters : Cluster list
        new(key : String, timestamp : Int64, clusters : Cluster list) = 
            { Key = key; Timestamp = timestamp; Clusters = clusters }
    end
    
    override x.Equals(yobj) = 
        match yobj with
        | :? ClusterSet as y -> (x.Timestamp = y.Timestamp)
        | _ -> false

    override x.GetHashCode() = hash(x)
    
    override x.ToString() =
        String.Format("'{0}' {1} clusters @ {2}", x.Key, Seq.length x.Clusters, x.Timestamp)

    interface IComparable with
        member x.CompareTo other = 
            match other with 
            | :? ClusterSet as y -> x.Timestamp.CompareTo(y.Timestamp)
            | _ -> invalidArg "other" "cannot compare value of different types" 