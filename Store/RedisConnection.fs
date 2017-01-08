namespace Myriad.Store

open System

open StackExchange.Redis
open StackExchange.Redis.KeyspaceIsolation

type RedisConnection(configuration : String) =
    let namespaceKey = RedisKey.op_Implicit("configuration/")
    let connection = ConnectionMultiplexer.Connect(configuration)
    let getDatabase() = connection.GetDatabase().WithKeyPrefix(namespaceKey)
    new() = new RedisConnection("localhost:6379")
    interface IDisposable with
        member x.Dispose() = x.Dispose()
    member x.Dispose() = connection.Dispose()
    member x.GetDatabase() = getDatabase()
