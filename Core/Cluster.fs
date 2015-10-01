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
        val Operation : Operation
        new(id : Int64, key : String, value : String, measures : Set<Measure>, timestamp : Int64, userName : String, operation : Operation) =
            { Id = id; Key = key; Value = value; Measures = measures; Timestamp = timestamp; UserName = userName; Operation = operation }
        new(id : Int64, key : String, value : String, measures : Set<Measure>, audit : IAudit) =
            { Id = id; Key = key; Value = value; Measures = measures; Timestamp = audit.Timestamp; UserName = audit.UserName; Operation = audit.Operation }
    end

    interface IAudit with
        member x.Timestamp with get() = x.Timestamp
        member x.UserName with get() = x.UserName
        member x.Operation with get() = x.Operation

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

[<CustomEquality;CustomComparison>]
type ClusterSet = 
    struct     
        //val Id : Int64        
        //val Key : String
        val Timestamp : Int64;
        val Clusters : Set<Cluster>
        new(timestamp : Int64, clusters : Set<Cluster>) = { Timestamp = timestamp; Clusters = clusters }
    end
    
    override x.Equals(yobj) = 
        match yobj with
        | :? ClusterSet as y -> (x.Timestamp = y.Timestamp)
        | _ -> false

    override x.GetHashCode() = hash(x)
    
    interface IComparable with
        member x.CompareTo other = 
            match other with 
            | :? ClusterSet as y -> x.Timestamp.CompareTo(y.Timestamp)
            | _ -> invalidArg "other" "cannot compare value of different types" 