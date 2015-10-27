namespace Myriad

open System

type IMeasure =
    abstract Dimension : IDimension with get
    abstract Value : String with get

/// Measures are equivalent over dimension id and value
[<CustomEquality;CustomComparison>]
type Measure = 
    { Dimension : IDimension; Value : String }
    
    interface IComparable with
        member x.CompareTo other = 
            match other with 
            | :? Measure as y -> Measure.CompareTo(x, y)
            | _ -> invalidArg "other" "cannot compare value of different types" 

    interface IMeasure with
        member x.Dimension with get() = x.Dimension
        member x.Value with get() = x.Value

    override x.ToString() = String.Format("'{0}' [{1}] = '{2}'", x.Dimension.Name, x.Dimension.Id, x.Value)

    override x.Equals(obj) = 
        match obj with
        | :? IMeasure as y -> Measure.CompareTo(x, y) = 0
        | _ -> false

    override x.GetHashCode() = hash(x.Dimension.Id, x.Value)
    
    static member CompareTo(x : IMeasure, y : IMeasure) = compare (x.Dimension.Id, x.Value) (y.Dimension.Id, y.Value)

    static member Create(dimensionId : Int64, dimensionName : String, value : String) =
        { Dimension = Dimension.Create(dimensionId, dimensionName); Value = value}

    static member Create(dimension : IDimension, value : String) = 
        { Dimension = dimension; Value = value}
