namespace Myriad

open System

/// Dimensions are equivalent over their ids
[<CustomEquality;CustomComparison>]
type Dimension =
    { Id : Int64 
      Name : String
      Timestamp : Int64
      UserName : String
      Operation : Operation    
    }
    
    interface IAudit with
        member x.Timestamp with get() = x.Timestamp
        member x.UserName with get() = x.UserName
        member x.Operation with get() = x.Operation

    interface IComparable with
        member x.CompareTo other = 
            match other with 
            | :? Dimension as y -> Dimension.CompareTo(x, y)
            | _ -> invalidArg "other" "cannot compare value of different types" 

    override x.ToString() = String.Concat("Dimension [", x.Name, "]")

    override x.Equals(obj) = 
        match obj with
        | :? Dimension as y -> Dimension.CompareTo(x, y) = 0
        | _ -> false

    override x.GetHashCode() = hash(x.Id)
    
    static member Create(id : Int64, name : String, audit : IAudit) =
        { Id = id; Name = name; Timestamp = audit.Timestamp; UserName = audit.UserName; Operation = audit.Operation }    

    static member CompareTo(x : Dimension, y : Dimension) = compare (x.Id) (y.Id)

