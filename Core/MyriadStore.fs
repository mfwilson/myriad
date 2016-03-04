namespace Myriad

open System
open System.Collections.Concurrent
open System.Collections.Generic

type MyriadHistory =
    | All of unit
    | Depth of int
    | Time of TimeSpan

type IMyriadStore =
    abstract Initialize : unit -> unit

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

    /// Add a value to list of possible values given a dimension and value.
    /// Returns true if the value was added; otherwise false.
    abstract AddMeasure : ``measure`` : Measure -> bool        
    /// Remove a value from the list of possible values for a dimension given a dimension and value.
    /// Returns true if the value was removed; otherwise false.
    abstract RemoveMeasure : ``measure`` : Measure -> bool

    abstract SetProperty : Property -> Property

    // Querying
    abstract GetProperties : MyriadHistory -> Property list
    abstract GetAny : String * Context -> (Property * Cluster) seq
    abstract GetMatches : String * Context -> (Property * Cluster) seq
    abstract GetProperty : String * DateTimeOffset -> Property option
    
    // Building
    abstract GetMeasureBuilder : unit -> MeasureBuilder
    abstract GetPropertyBuilder : unit -> PropertyBuilder


type MemoryStore() =
    let cache = new MyriadCache()
    
    let criticalSection = new Object()
    let dimensions = new List<Dimension>()
    let dimensionMap = new Dictionary<String, Dimension>(StringComparer.InvariantCultureIgnoreCase)
    let dimensionValues = new Dictionary<Dimension, SortedSet<String>>()

    let mutable currentId = 1L

    let getDimension (dimensionName : String) =
        lock criticalSection (fun () ->
            let success, value = dimensionMap.TryGetValue(dimensionName)
            if success then Some value else None
        )

    let addDimension (dimensionName : String) =
        lock criticalSection (fun () ->
            let success, value = dimensionMap.TryGetValue(dimensionName)
            if success then
                value
            else
                currentId <- currentId + 1L
                let newDimension = { Id = currentId; Name = dimensionName }
                dimensions.Add(newDimension)
                dimensionMap.[dimensionName] <- newDimension
                dimensionValues.[newDimension] <- new SortedSet<String>()
                newDimension                            
        )

    let removeDimension (dimension : Dimension) =
        lock criticalSection (fun () ->
            dimensions.Remove(dimension) && dimensionMap.Remove(dimension.Name) && dimensionValues.Remove(dimension)
        )

    let addMeasure (``measure`` : Measure) =
        lock criticalSection (fun () ->
            let success, value = dimensionValues.TryGetValue(``measure``.Dimension)
            if not success then
                false
            else
                value.Add(``measure``.Value)            
        )
        
    let removeMeasure (``measure`` : Measure) =
        lock criticalSection (fun () ->
            let success, value = dimensionValues.TryGetValue(``measure``.Dimension)
            if not success then
                false
            else
                value.Remove(``measure``.Value)            
        )

    let propertyDimension = addDimension "Property" 

    interface IMyriadStore with
        member x.Initialize() = x.Initialize()        
        member x.GetMetadata() = x.GetMetadata()
        member x.GetDimensions() = x.GetDimensions()    
        member x.GetDimension(dimensionName) = x.GetDimension(dimensionName)    
        member x.AddDimension(dimensionName) = x.AddDimension(dimensionName)
        member x.RemoveDimension(dimension) = x.RemoveDimension(dimension)
        member x.AddMeasure(``measure``) = x.AddMeasure(``measure``)
        member x.RemoveMeasure(``measure``) = x.RemoveMeasure(``measure``)
        member x.GetProperties(history) = x.GetProperties(history)
        member x.GetAny(propertyKey, context) = x.GetAny(propertyKey, context)
        member x.GetMatches(propertyKey, context) = x.GetMatches(propertyKey, context)
        member x.GetProperty(propertyKey, asOf) = x.GetProperty(propertyKey, asOf)
        member x.GetMeasureBuilder() = x.GetMeasureBuilder()
        member x.GetPropertyBuilder() = x.GetPropertyBuilder()
        member x.SetProperty(property) = x.SetProperty(property)

    member x.Initialize() =
        ignore()
        
    member x.GetMetadata() =         
        dimensionValues 
        |> Seq.map (fun kv -> { Dimension = kv.Key; Values = kv.Value |> Seq.toArray } ) 
        |> Seq.toList

    member x.GetDimensions() = dimensions |> Seq.toList

    member x.GetDimension(dimensionName : String) = getDimension dimensionName        

    member x.AddDimension(dimensionName : String) = addDimension dimensionName
        
    member x.RemoveDimension(dimension : Dimension) = removeDimension dimension

    member x.AddMeasure(``measure`` : Measure) = addMeasure ``measure``

    member x.RemoveMeasure(``measure`` : Measure) = removeMeasure ``measure`` 
    
    member x.GetProperties(history : MyriadHistory) = cache.GetProperties() |> Seq.toList

    member x.GetAny(propertyKey : String, context : Context) = 
        match propertyKey with
        | key when String.IsNullOrEmpty(key) -> cache.GetAny(context)
        | key -> cache.GetAny(key, context)
    
    member x.GetMatches(propertyKey : String, context : Context) = 
        match propertyKey with
        | key when String.IsNullOrEmpty(key) -> cache.GetMatches(context)
        | key -> 
            let success, result = cache.TryFind(key, context)
            if result.IsNone then Seq.empty else [ result.Value ] |> Seq.ofList        
    
    member x.GetProperty(propertyKey : String, asOf : DateTimeOffset) = 
        cache.GetProperty(propertyKey, asOf)

    member x.GetMeasureBuilder() = 
        let dimensions = x.GetDimensions()
        let dimensionMap = dimensions |> Seq.map (fun d -> d.Name, d) |> Map.ofSeq
        MeasureBuilder(dimensionMap)

    member x.GetPropertyBuilder() = 
        PropertyBuilder(x.GetDimensions())

    member x.SetProperty(property : Property) =
        addMeasure { Dimension = propertyDimension; Value = property.Key } |> ignore
        cache.Insert(property)
