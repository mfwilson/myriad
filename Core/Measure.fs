namespace Myriad

open System

/// Measures are equivalent over dimension id and value
[<CustomEquality;CustomComparison>]
type Measure = 
    struct
        val DimensionId : Int64
        val DimensionName : String
        val Value : String 
        new(dimensionId : Int64, dimensionName : String, value : String) = 
            { DimensionId = dimensionId; DimensionName = dimensionName; Value = value}
        new(dimension : Dimension, value : String) = 
            { DimensionId = dimension.Id; DimensionName = dimension.Name; Value = value}
    end

    interface IComparable with
        member x.CompareTo other = 
            match other with 
            | :? Measure as y -> Measure.CompareTo(x, y)
            | _ -> invalidArg "other" "cannot compare value of different types" 

    override x.ToString() = String.Format("'{0}' [{1}] = '{2}'", x.DimensionName, x.DimensionId, x.Value)

    override x.Equals(obj) = 
        match obj with
        | :? Measure as y -> Measure.CompareTo(x, y) = 0
        | _ -> false

    override x.GetHashCode() = hash(x.DimensionId, x.Value)
    
    static member CompareTo(x : Measure, y : Measure) = compare (x.DimensionId, x.Value) (y.DimensionId, y.Value)

