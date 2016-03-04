namespace Myriad

open System

[<CustomEquality;CustomComparison>]
type Property = 
    { Key : String; Description : String; Deprecated : bool; Timestamp : Int64; Clusters : Cluster list }
    
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

    static member Create(key : String, timestamp : Int64, clusters : Cluster list) =
        { Key = key; Description = ""; Deprecated = false; Timestamp = timestamp; Clusters = clusters }

    static member Create(key : String, description : String, deprecated : bool, timestamp : Int64, clusters : Cluster list) = 
        { Key = key; Description = description; Deprecated = deprecated; Timestamp = timestamp; Clusters = clusters }
