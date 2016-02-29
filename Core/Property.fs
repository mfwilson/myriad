namespace Myriad

open System

[<CustomEquality;CustomComparison>]
type Property = 
    struct
        val Key : String
        val Description : String
        val Deprecated : bool
        val Timestamp : Int64
        val Clusters : Cluster list
        new(key : String, description : String, deprecated : bool, timestamp : Int64, clusters : Cluster list) = 
            { Key = key; Description = description; Deprecated = deprecated; Timestamp = timestamp; Clusters = clusters }
        new(key : String, timestamp : Int64, clusters : Cluster list) = 
            { Key = key; Description = ""; Deprecated = false; Timestamp = timestamp; Clusters = clusters }
    end
    
    override x.Equals(yobj) = 
        match yobj with
        | :? Property as y -> (x.Key = y.Key && x.Timestamp = y.Timestamp)
        | _ -> false

    override x.GetHashCode() = hash(x)
    
    override x.ToString() =
        String.Format("'{0}' {1} clusters @ {2}", x.Key, Seq.length x.Clusters, x.Timestamp)

    interface IComparable with
        member x.CompareTo other = 
            match other with 
            | :? Property as y -> x.Timestamp.CompareTo(y.Timestamp)
            | _ -> invalidArg "other" "cannot compare value of different types" 