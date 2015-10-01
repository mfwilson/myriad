namespace Myriad

open System

/// Dimensions are equivalent over their ids
[<CustomEquality;CustomComparison>]
type Dimension =
    { Id : Int64; Name : String }
    
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
    
    static member Create(id : Int64, name : String) = { Id = id; Name = name }    

    static member CompareTo(x : Dimension, y : Dimension) = compare (x.Id) (y.Id)

