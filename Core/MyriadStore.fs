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

    let propertyDimension = addDimension "Property" 

    let removeDimension (dimension : Dimension) =
        lock criticalSection (fun () ->
            if dimension = propertyDimension then
                false
            else
                dimensions.Remove(dimension) && dimensionMap.Remove(dimension.Name) && dimensionValues.Remove(dimension)
        )

    let addMeasure (``measure`` : Measure) =
        lock criticalSection (fun () ->
            let success, value = dimensionValues.TryGetValue(``measure``.Dimension)
            if not success then
                None
            else
                value.Add(``measure``.Value) |> ignore
                Some({ Dimension = ``measure``.Dimension; Values = value |> Seq.toArray })
        )
        
    let removeMeasure (``measure`` : Measure) =
        lock criticalSection (fun () ->
            let success, value = dimensionValues.TryGetValue(``measure``.Dimension)
            if not success then
                false
            else
                value.Remove(``measure``.Value)            
        )

    let updateMeasures (property : Property) =
        lock criticalSection (fun () ->
            addMeasure { Dimension = propertyDimension; Value = property.Key } |> ignore
            let measures = property.Clusters |> List.map (fun c -> c.Measures) |> Set.unionMany 
            measures |> Set.map addMeasure |> ignore
        )

    let setDimensionOrder (orderedDimensions : Dimension list) =        
        let pb = PropertyBuilder(orderedDimensions)        
        let keys = cache.Keys
        let properties = keys 
                            |> Seq.map (fun k -> cache.[k].Value.Head) 
                            |> Seq.toList
                            |> List.map pb.ApplyDimensionOrder
        properties |> Seq.iter (fun p -> cache.SetProperty p |> ignore) 

        lock criticalSection (fun () ->            
            dimensions.Clear()
            dimensions.AddRange(orderedDimensions)                        
            orderedDimensions
        )

    interface IMyriadStore with
        member x.Initialize() = x.Initialize()        
        member x.GetMetadata() = x.GetMetadata()
        member x.GetDimensions() = x.GetDimensions()    
        member x.GetDimension(dimensionName) = x.GetDimension(dimensionName)    
        member x.AddDimension(dimensionName) = x.AddDimension(dimensionName)
        member x.RemoveDimension(dimension) = x.RemoveDimension(dimension)
        member x.SetDimensionOrder(dimensions) = x.SetDimensionOrder(dimensions)
        member x.AddMeasure(``measure``) = x.AddMeasure(``measure``)
        member x.RemoveMeasure(``measure``) = x.RemoveMeasure(``measure``)
        member x.GetProperties(history) = x.GetProperties(history)
        member x.GetAny(propertyKey, context) = x.GetAny(propertyKey, context)
        member x.GetMatches(propertyKey, context) = x.GetMatches(propertyKey, context)
        member x.GetProperty(propertyKey, asOf) = x.GetProperty(propertyKey, asOf)
        member x.GetMeasureBuilder() = x.GetMeasureBuilder()
        member x.GetPropertyBuilder() = x.GetPropertyBuilder()
        member x.SetProperty(property) = x.SetProperty(property)
        member x.PutProperty(property) = x.PutProperty(property)

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
    
    member x.SetDimensionOrder(orderedDimensions : Dimension list) =       
        let current = dimensions |> Set.ofSeq
        let proposed = orderedDimensions |> Set.ofList
        // If this is not the same set, we cannot reorder
        if current <> proposed then dimensions |> List.ofSeq else setDimensionOrder orderedDimensions                       

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
        updateMeasures property
        cache.SetProperty property

    member x.PutProperty(value : PropertyOperation) =
        let pb = x.GetPropertyBuilder()
        let filter = [ propertyDimension ]
        
        let add (key : string) = 
            let property = value.ToProperty(pb.OrderClusters, filter)
            new LockFreeList<Property>( [ property ] ) 

        let update (key : string) (current : LockFreeList<Property>) = 
            let currentProperty = current.Value.Head
            let filterMeasures(cluster) = PropertyOperation.FilterMeasures(cluster, filter)
            let applyOperations(current : Cluster list) (operation : Operation<Cluster>) =
                match operation with
                | Add(cluster) -> filterMeasures(cluster) :: current
                | Update(previous, updated) -> filterMeasures(updated) :: (current |> List.filter (fun c -> c <> previous))
                | Remove(cluster) -> current |> List.filter (fun c -> c <> cluster)
            let clusters = pb.OrderClusters (value.Operations |> List.fold applyOperations currentProperty.Clusters) 
            let property = Property.Create(currentProperty.Key, value.Description, value.Deprecated, value.Timestamp, clusters)
            current.Add property        

        let current = cache.AddOrUpdate(value.Key, add, update)
        let property = current.Value.Head
        updateMeasures property
        property


