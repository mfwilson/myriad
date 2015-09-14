namespace Myriad.Store

open System

open Newtonsoft.Json

open StackExchange.Redis
open StackExchange.Redis.KeyspaceIsolation

open Myriad

type RedisConnection(configuration : String) = 

    let connection = ConnectionMultiplexer.Connect(configuration)

    let namespaceKey = RedisKey.op_Implicit("configuration/")

    let updateUser = Environment.UserName

    let getDatabase() = connection.GetDatabase().WithKeyPrefix(namespaceKey)

    let getKey(objectType : String, objectId : String option) =
        if objectId.IsNone then
            RedisKey.op_Implicit(objectType)
        else
            RedisKey.op_Implicit(String.Concat(objectType, "/", objectId.Value)) 

    new() = new RedisConnection("localhost:6379")

    interface IDisposable with
        member x.Dispose() = connection.Dispose()

    member private x.GetId() = 
        let database = getDatabase()
        let key = getKey("transaction_id", None)
        database.StringIncrement(key)

    member x.GetDimensions() =         
        let database = getDatabase()
        let key = getKey("dimension", None)

        let value = database.SortedSetRangeByScore(key, order = Order.Descending, take = 1L) |> Array.tryHead
        if value.IsNone then
            Array.empty
        else
            let json = value.Value.ToString()
            JsonConvert.DeserializeObject<Dimension array>(json)

    member x.SetDimensions(dimensions : Dimension seq) =         
        let database = getDatabase()
        let key = getKey("dimension", None)        
        let score = float DateTimeOffset.Now.UtcTicks
        let dimensionsJson = JsonConvert.SerializeObject(dimensions)
        database.SortedSetAdd(key, RedisValue.op_Implicit(dimensionsJson), score)

    member x.CreateDimension(name : String) =
        let id = x.GetId()
        let audit = { Timestamp = DateTimeOffset.Now.UtcTicks; UpdateUser = updateUser; Operation = Operation.Create }
        let dimension = { Dimension.Id = id; Name = name; Audit = audit }
        let dimensionJson = JsonConvert.SerializeObject(dimension)
        let database = getDatabase()
        let key = getKey("dimension", Some(id.ToString()))
        let result = database.SortedSetAdd(key, RedisValue.op_Implicit(dimensionJson), float audit.Timestamp)        
        dimension        

    member x.GetDimensionValues(dimensions : Dimension seq) =
        let database = getDatabase()

        let getValues(dimension : Dimension) =
            let key = getKey("dimension", Some(dimension.Name))
            let members = database.SetMembers(key) |> Array.map (fun v -> v.ToString())
            dimension.Name, members

        dimensions |> Seq.map getValues |> Map.ofSeq

    member x.GetProperty(property : String, asOf : DateTimeOffset) =
        let database = getDatabase()

        let key = getKey("property", Some(property))
        
        ignore()

    member x.CreateCluster(property : Property, value : String, measures : Set<Measure>) =
        let id = x.GetId()
        let audit = { Timestamp = DateTimeOffset.Now.UtcTicks; UpdateUser = updateUser; Operation = Operation.Create }
        let cluster = new Cluster(id, property, value, measures, audit)
        let clusterJson = JsonConvert.SerializeObject(cluster)

        let database = getDatabase()
        let key = getKey("cluster", Some(id.ToString()))
        
        let result = database.SortedSetAdd(key, RedisValue.op_Implicit(clusterJson), float audit.Timestamp)
        cluster
    
    member x.UpdateCluster(id : Int64, value : String, measures : Set<Measure>) =    
        let audit = { Timestamp = DateTimeOffset.Now.UtcTicks; UpdateUser = updateUser; Operation = Operation.Create }

        let database = getDatabase()
        let key = getKey("cluster", Some(id.ToString()))
        
        let h = database.SortedSetRangeByScore(key, order=Order.Descending, take=1L)
                
        0