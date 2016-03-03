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

    let getCurrentTimestamp() = Epoch.GetOffset(DateTimeOffset.UtcNow.Ticks)

    let getAudit() = Audit.Create(getCurrentTimestamp(), updateUser)

    let getDatabase() = connection.GetDatabase().WithKeyPrefix(namespaceKey)

    let getKey(objectType : String, objectId : String option) =
        if objectId.IsNone then
            RedisKey.op_Implicit(objectType)
        else
            RedisKey.op_Implicit(String.Concat(objectType, "/", objectId.Value)) 

    new() = new RedisConnection("localhost:6379")

    interface IDisposable with
        member x.Dispose() = connection.Dispose()

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