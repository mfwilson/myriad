namespace Myriad.Store

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Configuration
open System.Data
open System.Diagnostics
open System.Runtime.InteropServices
open System.Text
open System.Threading

open Myriad        

type MySqlStore(connectionString : String) =
    static let ts = new TraceSource( "Myriad.Store", SourceLevels.Information )
    let cache = new MyriadCache()        
    let mutable timestamp = 0L

    // Changing the dimensions requires restart
    let dimensions = MySqlAccessor.getDimensions connectionString       

    let dimensionMap = 
        let value = new Dictionary<String, Dimension>(StringComparer.InvariantCultureIgnoreCase)
        dimensions |> List.iter (fun d -> value.[d.Name] <- d)
        value

    let propertyDimension = 
        let success, dimension = dimensionMap.TryGetValue("Property")        
        if success then dimension else raise(new Exception("Check database setup, cannot find Property dimension.")) 

    let addMeasure (``measure`` : Measure) =
        MySqlAccessor.addMeasure connectionString ``measure`` 

    let addProperty (property : Property) =
        let propertyMeasure = { Dimension = propertyDimension; Value = property.Key }
        MySqlAccessor.addProperty connectionString propertyMeasure property.Deprecated property.Description

    let addPropertyOperation (value : PropertyOperation) =
        let propertyMeasure = { Dimension = propertyDimension; Value = value.Key }
        MySqlAccessor.addProperty connectionString propertyMeasure value.Deprecated value.Description

    let getTimestamp() = Interlocked.Read(&timestamp)
    
    let updateTimestamp (newTimestamp : int64) =
        if getTimestamp() < newTimestamp then
            Interlocked.Exchange(&timestamp, newTimestamp) |> ignore
            let datetime = Epoch.ToDateTimeOffset(newTimestamp)
            ts.TraceEvent(TraceEventType.Information, 0, "Latest timestamp: [{0}] ({1})", newTimestamp, Epoch.FormatDateTimeOffset(datetime))

    let setProperties (properties : Property list) =
        properties |> List.iter (fun p -> cache.SetProperty p |> ignore) 
        if properties.Length > 0 then
            ts.TraceEvent(TraceEventType.Information, 0, "Set {0} properties; {1} unique keys.", properties.Length, cache.Count)
            updateTimestamp properties.[properties.Length - 1].Timestamp

    let updatePropertiesByTimeAndLatest (timespan : TimeSpan) =        
        let cutoff = DateTimeOffset.UtcNow.Subtract(timespan)
        let asOf = Epoch.GetOffset(cutoff.Ticks)
        ts.TraceEvent(TraceEventType.Information, 0, "Loading properties from asOf: [{0}] ({1})", asOf, Epoch.FormatDateTimeOffset(cutoff))
        MySqlAccessor.getRecentProperties connectionString asOf |> setProperties

    /// Get latest properties from properties table
    let updateProperties() =        
        MySqlAccessor.getProperties connectionString (getTimestamp()) |> setProperties

    new() =
        let connectionString = ConfigurationManager.AppSettings.["mysqlConnection"]
        MySqlStore(connectionString)

    interface IMyriadStore with
        member x.Initialize(history) = x.Initialize(history)
        member x.GetMetadata() = x.GetMetadata()
        member x.GetDimensions() = x.GetDimensions()    
        member x.GetDimension(dimensionName) = x.GetDimension(dimensionName)    
        member x.AddDimension(dimensionName) = x.AddDimension(dimensionName)
        member x.RemoveDimension(dimension) = x.RemoveDimension(dimension)
        member x.SetDimensionOrder(dimensions) = x.SetDimensionOrder(dimensions)
        member x.AddMeasure(``measure``) = x.AddMeasure(``measure``)
        member x.RemoveMeasure(``measure``) = x.RemoveMeasure(``measure``)
        member x.GetProperties() = x.GetProperties()
        member x.GetAny(propertyKey, context) = x.GetAny(propertyKey, context)
        member x.GetMatches(propertyKey, context) = x.GetMatches(propertyKey, context)
        member x.GetProperty(propertyKey, asOf) = x.GetProperty(propertyKey, asOf)
        member x.GetMeasureBuilder() = x.GetMeasureBuilder()
        member x.GetPropertyBuilder() = x.GetPropertyBuilder()
        member x.SetProperty(property) = x.SetProperty(property)
        member x.PutProperty(property) = x.PutProperty(property)

    member x.Initialize(history : MyriadHistory) =
        ts.TraceEvent(TraceEventType.Information, 0, sprintf "Initializing MySql store with %A" history)
        match history with
        | All() -> updateProperties()
        | TimeAndLatest(t) -> updatePropertiesByTimeAndLatest t
        
    member x.GetMetadata() =         
        let values = MySqlAccessor.getMetadata connectionString
        let map = new Dictionary<Dimension, List<String>>()
        let add(pair : Dimension * String) =            
            let success, values = map.TryGetValue(fst(pair))
            match success with
            | false -> map.[fst(pair)] <- new List<String>([snd(pair)])
            | true -> values.Add(snd(pair))                
        values |> Seq.iter add

        map
        |> Seq.map (fun kv -> { Dimension = kv.Key; Values = kv.Value |> Seq.toArray } ) 
        |> Seq.toList        

    member x.GetDimensions() = dimensions

    member x.GetDimension(dimensionName : String) = 
        let success, result = dimensionMap.TryGetValue(dimensionName)
        if success then Some result else None        
        
    member x.AddDimension(dimensionName : String) = 
        let dimensionId = MySqlAccessor.addDimension connectionString dimensionName 
        { Id = dimensionId; Name = dimensionName }
                
    member x.RemoveDimension(dimension : Dimension) = 
        MySqlAccessor.removeDimension connectionString dimension.Id > 0

    member x.AddMeasure(``measure`` : Measure) =
        addMeasure ``measure`` |> ignore
        let values = MySqlAccessor.getDimensionValues connectionString ``measure``.Dimension.Id
        Some({ Dimension = ``measure``.Dimension; Values = values |> List.toArray })

    member x.RemoveMeasure(``measure`` : Measure) = 
        MySqlAccessor.removeMeasure connectionString ``measure`` > 0
    
    member x.SetDimensionOrder(orderedDimensions : Dimension list) =       
        // TODO: Implement dimension ordering
        orderedDimensions
//        let current = store.Dimensions |> Set.ofSeq
//        let proposed = orderedDimensions |> Set.ofList
//        // If this is not the same set, we cannot reorder
//        if current <> proposed then store.Dimensions |> List.ofSeq else setDimensionOrder orderedDimensions                       

    member x.GetProperties() = 
        updateProperties()    
        cache.GetProperties() |> Seq.toList        

    member x.GetAny(propertyKey : String, context : Context) = 
        updateProperties()
        match propertyKey with
        | key when String.IsNullOrEmpty(key) -> cache.GetAny(context)
        | key -> cache.GetAny(key, context)
    
    member x.GetMatches(propertyKey : String, context : Context) = 
        updateProperties()
        match propertyKey with
        | key when String.IsNullOrEmpty(key) -> cache.GetMatches(context)
        | key -> 
            let success, result = cache.TryFind(key, context)
            if result.IsNone then Seq.empty else [ result.Value ] |> Seq.ofList        
    
    member x.GetProperty(propertyKey : String, asOf : DateTimeOffset) = 
        updateProperties()
        cache.GetProperty(propertyKey, asOf)

    member x.GetMeasureBuilder() = 
        let dimensions = x.GetDimensions()
        let dimensionMap = dimensions |> Seq.map (fun d -> d.Name, d) |> Map.ofSeq
        MeasureBuilder(dimensionMap)

    member x.GetPropertyBuilder() = 
        PropertyBuilder(x.GetDimensions())

    member x.SetProperty(property : Property) =
        let measureId = addProperty property
        updateProperties()
        x.UpdateMeasures property measureId
        property

    member x.PutProperty(value : PropertyOperation) =        
        let pb = x.GetPropertyBuilder()
        let filter = [ propertyDimension ]
        
        let measureId = addPropertyOperation value
        
        let add (key : string) = 
            value.ToProperty(pb.OrderClusters, filter)
            
        let update (key : string) (currentProperty : Property) = 
            let filterMeasures(cluster) = PropertyOperation.FilterMeasures(cluster, filter)
            let applyOperations(current : Cluster list) (operation : Operation<Cluster>) =
                match operation with
                | Add(cluster) -> filterMeasures(cluster) :: current
                | Update(previous, updated) -> filterMeasures(updated) :: (current |> List.filter (fun c -> c <> previous))
                | Remove(cluster) -> current |> List.filter (fun c -> c <> cluster)
            let clusters = pb.OrderClusters (value.Operations |> List.fold applyOperations currentProperty.Clusters) 
            Property.Create(value.Key, value.Description, value.Deprecated, value.Timestamp, clusters)       

        let property = MySqlAccessor.addOrUpdateProperty connectionString value.Key measureId add update
        if property.IsNone then raise(new Exception("Unable to put property to database."))
        updateProperties()  
        x.UpdateMeasures property.Value measureId
        property.Value

    member private x.UpdateMeasures (property : Property) (measureId : uint64) =
        MySqlAccessor.putProperty connectionString measureId property
        let measures = property.Clusters |> List.map (fun c -> c.Measures) |> Set.unionMany 
        measures |> Set.iter (fun m -> MySqlAccessor.addMeasure connectionString m |> ignore)
