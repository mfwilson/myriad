namespace Myriad.Store

open System

open StackExchange.Redis
open StackExchange.Redis.KeyspaceIsolation

open Myriad

type RedisConnection(configuration : String) = 

    let connection = ConnectionMultiplexer.Connect(configuration)

    let namespaceKey = RedisKey.op_Implicit("configuration/")

    let getDatabase() = connection.GetDatabase().WithKeyPrefix(namespaceKey)

    let getKey (objectType : String, objectId : String option) =
        if objectId.IsNone then
            RedisKey.op_Implicit(objectType)
        else
            RedisKey.op_Implicit(String.Concat(objectType, "/", objectId.Value)) 

    new() = new RedisConnection("localhost:6379")

    interface IDisposable with
        member x.Dispose() = connection.Dispose()

    //member x.GetNextId() = "configuration/transaction_id"

    member x.GetDimensions() =         
        let database = getDatabase()
        let key = getKey("dimension", None)
        let listLength = database.ListLength(key)        
        let dimensions = database.ListRange(key, 0L, listLength)              
        dimensions |> Array.map (fun v -> { Dimension.Id = 0; Name = v.ToString() } )

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
