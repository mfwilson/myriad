namespace Myriad.Store

open System
open System.Diagnostics

open Newtonsoft.Json
open StackExchange.Redis
open Myriad

module RedisAccessor =
    let private ts = new TraceSource( "Myriad.Store", SourceLevels.Information )

    let getKey(objectType : String) (objectId : String option) =
        match objectId with
        | Some o -> RedisKey.op_Implicit(String.Concat(objectType, "/", objectId.Value)) 
        | None -> RedisKey.op_Implicit(objectType)            

    let getDimensions(connection : RedisConnection) =
        let database = connection.GetDatabase()
        let key = getKey "dimensions" None
        let redis = database.SortedSetRangeByScore(key, order = Order.Descending, take = 1L) |> Array.tryPick Some
        if redis.IsNone then [] else JsonConvert.DeserializeObject<Dimension list>(redis.Value.ToString())

    /// Get all the latest values of all properties and any property set after asof 
    let getRecentProperties(connection : RedisConnection) (asOf : int64) = 
        ignore()

    let getProperties(connection : RedisConnection) (asOf : int64) = 

        // TODO: Pull properties after a point in time (as of) and convert to property list

        ignore()