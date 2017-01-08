namespace Myriad.Store

open System
open System.Collections.Generic
open System.Configuration
open System.Diagnostics
open System.Threading

open Newtonsoft.Json

open StackExchange.Redis
open StackExchange.Redis.KeyspaceIsolation

open Myriad

type RedisStore(configuration : String) = 
    static let ts = new TraceSource( "Myriad.Store", SourceLevels.Information )
    let cache = new MyriadCache()
    let mutable timestamp = 0L

    let store = new MyriadStore()    

    let connection = new RedisConnection(configuration)

    // Changing the dimensions requires restart
    let dimensions = RedisAccessor.getDimensions connection

    let dimensionMap = 
        let value = new Dictionary<String, Dimension>(StringComparer.InvariantCultureIgnoreCase)
        dimensions |> List.iter (fun d -> value.[d.Name] <- d)
        value

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
        RedisAccessor.getRecentProperties connection asOf //|> setProperties

    /// Get latest properties from properties table
    let updateProperties() =        
        RedisAccessor.getProperties connection (getTimestamp()) //|> setProperties


    new() = 
        let configuration = ConfigurationManager.AppSettings.["redisConnection"]
        new RedisStore(configuration)

    interface IDisposable with
        member x.Dispose() = connection.Dispose()

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
        ts.TraceEvent(TraceEventType.Information, 0, sprintf "Initializing Redis store with %A" history)
        match history with
        | All() -> updateProperties()
        | TimeAndLatest(t) -> updatePropertiesByTimeAndLatest t        

    member x.GetMetadata() = store.GetMetadata()       

    member x.GetDimensions() = dimensions

    member x.GetDimension(dimensionName : String) = store.GetDimension dimensionName        

    member x.AddDimension(dimensionName : String) = 
        let newId(name : String) = MySqlAccessor.addDimension configuration name
        store.AddDimension dimensionName newId
        
    member x.RemoveDimension(dimension : Dimension) = store.RemoveDimension dimension

    member x.AddMeasure(``measure`` : Measure) = 
        //MySqlAccessor.addMeasure connectionString ``measure`` |> ignore
        store.AddMeasure ``measure``

    member x.RemoveMeasure(``measure`` : Measure) = store.RemoveMeasure ``measure`` 
    
    member x.SetDimensionOrder(orderedDimensions : Dimension list) =       
        // TODO: Implement dimension ordering
        orderedDimensions
//        let current = store.Dimensions |> Set.ofSeq
//        let proposed = orderedDimensions |> Set.ofList
//        // If this is not the same set, we cannot reorder
//        if current <> proposed then store.Dimensions |> List.ofSeq else setDimensionOrder orderedDimensions                       

    member x.GetProperties() = cache.GetProperties() |> Seq.toList        

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
        store.UpdateMeasures property |> ignore
        cache.SetProperty property

    member x.PutProperty(value : PropertyOperation) =
        let pb = x.GetPropertyBuilder()
        let filter = [ store.PropertyDimension ]
        
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
        store.UpdateMeasures property |> ignore
        property


//    let connection = ConnectionMultiplexer.Connect(configuration)
//
//    let namespaceKey = RedisKey.op_Implicit("configuration/")
//
//    let updateUser = Environment.UserName
//
//    let getCurrentTimestamp() = Epoch.GetOffset(DateTimeOffset.UtcNow.Ticks)
//
//    let getAudit() = Audit.Create(getCurrentTimestamp(), updateUser)
//
//    let getDatabase() = connection.GetDatabase().WithKeyPrefix(namespaceKey)
//
//    let getKey(objectType : String, objectId : String option) =
//        if objectId.IsNone then
//            RedisKey.op_Implicit(objectType)
//        else
//            RedisKey.op_Implicit(String.Concat(objectType, "/", objectId.Value)) 


//    member private x.GetId() = 
//        let database = getDatabase()
//        let key = getKey("transaction_id", None)
//        database.StringIncrement(key)
//
//    member x.GetDimensions() =
//        let database = getDatabase()
//        let key = getKey("dimensions", None)
//
//        //let value = database.SortedSetRangeByScore(key, order = Order.Descending, take = 1L) |> Array.tryHead
//        //if value.IsNone then
//        List.empty
//        //else
//        //    let json = value.Value.ToString()
//        //    JsonConvert.DeserializeObject<Dimension list>(json)
//
//    member x.SetDimensions(dimensions : Dimension seq) =
//        let database = getDatabase()
//        let key = getKey("dimensions", None)
//        let score = float (getCurrentTimestamp())
//        let dimensionsJson = JsonConvert.SerializeObject(dimensions)
//        database.SortedSetAdd(key, RedisValue.op_Implicit(dimensionsJson), score)
//
//    member x.CreateDimension(name : String) =
//        let id = x.GetId()
//        let dimension = Dimension.Create(id, name (*, getAudit(Operation.Create)*))
//
//        //let dimensionJson = JsonConvert.SerializeObject(dimension)
//        //let database = getDatabase()
//        //let key = getKey("dimensions", Some(id.ToString()))
//        //let result = database.SetAdd(key, RedisValue.op_Implicit(dimensionJson), float dimension.Timestamp)
//        dimension
//
//    member x.AddDimensionValues(dimension : Dimension, values : String seq) =
//        let database = getDatabase()
//        let key = getKey("dimensions", Some(dimension.Id.ToString()))
//        values |> Seq.iter (fun value -> database.SetAdd(key, RedisValue.op_Implicit(value)) |> ignore)
//        
//    member x.RemoveDimensionValues(dimension : Dimension, values : String seq) =
//        let database = getDatabase()
//        let key = getKey("dimensions", Some(dimension.Id.ToString()))
//        values |> Seq.iter (fun value -> database.SetRemove(key, RedisValue.op_Implicit(value)) |> ignore)        
//
//    member x.GetDimensionValues(dimensions : Dimension seq) =
//        let database = getDatabase()
//
//        let getValues(dimension : Dimension) =
//            let key = getKey("dimensions", Some(dimension.Id.ToString()))
//            let members = database.SetMembers(key) |> Array.map (fun v -> v.ToString())
//            dimension.Name, members
//
//        dimensions |> Seq.map getValues |> Map.ofSeq
//
//    member x.GetProperty(property : String, asOf : DateTimeOffset) =
//        let database = getDatabase()
//
//        let key = getKey("property", Some(property))
//        
//        ignore()
//
//    member x.CreateCluster(key : String, value : String, measures : Set<Measure>) =
//        let id = x.GetId()
//        let cluster = Cluster.Create((*id,*) value, measures)
//        let clusterJson = JsonConvert.SerializeObject(cluster)
//
//        let database = getDatabase()
//        let key = getKey("cluster", Some(id.ToString()))
//        
//        let result = database.SortedSetAdd(key, RedisValue.op_Implicit(clusterJson), float cluster.Timestamp)
//        cluster
//    
//    member x.UpdateCluster(id : Int64, value : String, measures : Set<Measure>) =    
//        //let audit = Audit(getCurrentTimestamp(), updateUser, Operation.Create)
//
//        let database = getDatabase()
//        let key = getKey("cluster", Some(id.ToString()))
//        
//        let h = database.SortedSetRangeByScore(key, order=Order.Descending, take=1L)
//                
//        0