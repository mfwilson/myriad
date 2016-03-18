namespace Myriad

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Diagnostics

type MyriadHistory =
    | All of unit
    | TimeAndLatest of TimeSpan
    //| Depth of int
    //| Time of TimeSpan
    //| TimeAndDepth of TimeSpan * int

type IMyriadStore =
    abstract Initialize : MyriadHistory -> unit

    /// Return list of name+values where name is a dimension name and values contains the list of possible values
    abstract GetMetadata : unit -> DimensionValues list 

    /// Get the list of dimensions 
    abstract GetDimensions : unit -> Dimension list

    /// Get a dimension by name
    abstract GetDimension : String -> Dimension option

    /// Create a new dimension and append to list of dimensions 
    abstract AddDimension : String -> Dimension  
    /// Remove a dimension from the list of dimensions
    abstract RemoveDimension : Dimension -> bool
    /// Remove a dimension from the list of dimensions
    abstract SetDimensionOrder : Dimension list -> Dimension list

    /// Add a value to list of possible values given a dimension and value.
    /// Returns true if the value was added; otherwise false.
    abstract AddMeasure : ``measure`` : Measure -> DimensionValues option       
    /// Remove a value from the list of possible values for a dimension given a dimension and value.
    /// Returns true if the value was removed; otherwise false.
    abstract RemoveMeasure : ``measure`` : Measure -> bool       

    /// Overwrites the current property with the passed property data 
    abstract SetProperty : Property -> Property

    /// Merges the passed property data into the current property and returns the result
    abstract PutProperty : PropertyOperation -> Property

    // Querying
    abstract GetProperties : unit -> Property list
    abstract GetAny : String * Context -> (Property * Cluster) seq
    abstract GetMatches : String * Context -> (Property * Cluster) seq
    abstract GetProperty : String * DateTimeOffset -> Property option
    
    // Building
    abstract GetMeasureBuilder : unit -> MeasureBuilder
    abstract GetPropertyBuilder : unit -> PropertyBuilder


type MyriadStore() =
    static let ts = new TraceSource( "Myriad.Core", SourceLevels.Information )

    let criticalSection = new Object()
    let dimensions = new List<Dimension>()
    let dimensionMap = new Dictionary<String, Dimension>(StringComparer.InvariantCultureIgnoreCase)
    let dimensionValues = new Dictionary<Dimension, SortedSet<String>>()

    let addDimension (dimensionName : String) (newId : String -> uint64) =
        lock criticalSection (fun () ->
            let success, value = dimensionMap.TryGetValue(dimensionName)
            if success then
                value
            else
                let newDimension = { Id = newId(dimensionName); Name = dimensionName }
                dimensions.Add(newDimension)
                dimensionMap.[dimensionName] <- newDimension
                dimensionValues.[newDimension] <- new SortedSet<String>()
                ts.TraceEvent(TraceEventType.Information, 0, "Added dimension: [{0}]", dimensionName)
                newDimension                            
        )   

    let propertyDimension = addDimension "Property" (fun d -> 1UL)

    member x.GetDimension (dimensionName : String) =
        lock criticalSection (fun () ->
            let success, value = dimensionMap.TryGetValue(dimensionName)
            if success then Some value else None
        )

    member x.RemoveDimension (dimension : Dimension) =
        lock criticalSection (fun () ->
            if dimension = propertyDimension then
                false
            else
                let removed = dimensions.Remove(dimension) && dimensionMap.Remove(dimension.Name) && dimensionValues.Remove(dimension)
                if removed then ts.TraceEvent(TraceEventType.Information, 0, "Removed dimension: [{0}]", dimension.Name)
                removed
        )

    member x.AddMeasure (``measure`` : Measure) =
        lock criticalSection (fun () ->
            let success, value = dimensionValues.TryGetValue(``measure``.Dimension)
            if not success then
                None
            else
                if value.Add(``measure``.Value) then
                    ts.TraceEvent(TraceEventType.Information, 0, "Added measure: [{0}/{1}]", ``measure``.Dimension.Name, ``measure``.Value)
                Some({ Dimension = ``measure``.Dimension; Values = value |> Seq.toArray })
        )
        
    member x.RemoveMeasure (``measure`` : Measure) =
        lock criticalSection (fun () ->
            let success, value = dimensionValues.TryGetValue(``measure``.Dimension)
            if not success then
                false
            else
                let removed = value.Remove(``measure``.Value)            
                if removed then ts.TraceEvent(TraceEventType.Information, 0, "Removed measure: [{0}/{1}]", ``measure``.Dimension.Name, ``measure``.Value)
                removed
        )

    member x.UpdateMeasures (property : Property) =
        lock criticalSection (fun () ->
            x.AddMeasure { Dimension = propertyDimension; Value = property.Key } |> ignore
            let measures = property.Clusters |> List.map (fun c -> c.Measures) |> Set.unionMany 
            measures |> Set.map x.AddMeasure |> ignore
            measures
        )

    member x.SetDimensionOrder (orderedDimensions : Dimension list) = 
        lock criticalSection (fun () ->            
            dimensions.Clear()
            dimensions.AddRange(orderedDimensions)                        
            orderedDimensions
        )

    member x.GetMetadata() =         
        dimensionValues 
        |> Seq.map (fun kv -> { Dimension = kv.Key; Values = kv.Value |> Seq.toArray } ) 
        |> Seq.toList

    member x.GetDimensions() = dimensions |> Seq.toList    
    member x.AddDimension(dimensionName : String) (newId : String -> uint64) = addDimension dimensionName newId
    member x.PutDimension(dimension : Dimension) = addDimension dimension.Name (fun n -> dimension.Id)

    member x.PropertyDimension with get() = propertyDimension
    member x.Dimensions with get() = dimensions

