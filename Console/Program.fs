module Myriad.Test

open System
open System.IO
open System.Diagnostics

open Newtonsoft.Json
open Newtonsoft.Json.Serialization

open Myriad
open Myriad.Store

let hasArguments args flags = 
    args 
    |> Seq.choose(fun (a : string) -> if Seq.exists (fun f -> a.Contains(f)) flags then Some(a) else None)
    |> Seq.length > 0

let getArguments args flags =
    args 
    |> Seq.choose(fun (a : string) -> if Seq.exists (fun f -> a.Contains(f)) flags then Some(a) else None)
    |> Seq.map (fun (a : string) -> a.Substring(a.IndexOf(':') + 1))

let getArgument args flags defaultValue =
    let result = getArguments args flags 
    if Seq.isEmpty result then defaultValue else Seq.head result 

let print (color : System.ConsoleColor) (depth : int) (text : string) =
    let current = Console.ForegroundColor
    let indent = new String(' ', depth * 4)
    Console.ForegroundColor <- color
    Console.WriteLine(indent + text)
    Console.ForegroundColor <- current


// Environment -> PROD, UAT, DEV
// Location -> Chicago, New York, London, Amsterdam
// Application -> Rook, Knight, Pawn, Bishop
// Instance -> mary, jimmy, rex
let getDimensions() =
    [ { Dimension.Id = 32L; Name = "Environment" };
      { Dimension.Id = 21L; Name = "Location" };
      { Dimension.Id = 44L; Name = "Application" };
      { Dimension.Id = 98L; Name = "Instance" } ] 
    |> Seq.cast<IDimension>

let getClusters(mb : MeasureBuilder) =
    let key = "my.property.key"
    Seq.ofList
        [
            Cluster(0L, key, "apple", mb { yield "Environment", "PROD"; yield "Location", "London"; yield "Instance", "rex" } )
            Cluster(0L, key, "pear", mb { yield "Environment", "PROD"; yield "Instance", "rex" } )
            Cluster(0L, key, "pecan", mb { yield "Instance", "rex" } )
            Cluster(0L, key, "peach", mb { yield "Application", "Rook" } )
            Cluster(0L, key, "strawberry", mb { yield "Location", "Chicago" } )
            Cluster(0L, key, "apricot", mb { yield "Environment", "DEV" } )
            Cluster(0L, key, "pumpkin", Set.empty )
        ]

// Set initial dimensions
(*
    let redis = new RedisConnection()
    let dimensions = [ "Environment"; "Other"; "Location"; "Application"; "Instance" ]
                        |> List.map (fun e -> redis.CreateDimension(e))
    redis.SetDimensions(dimensions) |> ignore

    let read = redis.GetDimensions()
*)

// Set new dimensions
(*
    let redis = new RedisConnection()
    let read = redis.GetDimensions()
    let newDimension = redis.CreateDimension("NewDim")
    let newDimensions = newDimension :: read 
    redis.SetDimensions(newDimensions) |> ignore
*)

let scriptEntry(args) = 
    try
        let dimensions = getDimensions()
        let dimensionMap = dimensions |> Seq.map (fun d -> d.Name, d) |> Map.ofSeq
        let mb = new MeasureBuilder(dimensionMap)

        let setBuilder = new ClusterSetBuilder(dimensions)

        let clusterSet = setBuilder.Create(getClusters(mb))

        let cache = new MyriadCache()
        cache.Insert(clusterSet)


        //////

        let redis = new RedisConnection()

        let dimensions = redis.GetDimensions()

        redis.AddDimensionValues(dimensions.[0], [ "PROD"; "UAT"; "DEV" ] )
        
        let g = redis.GetDimensionValues(dimensions)

        //let d1 = redis.CreateDimension("Environment")
        

//        let dimensions = getDimensions()
//        let dimensionMap = dimensions |> Seq.map (fun d -> d.Name, d) |> Map.ofSeq
//        let mb = new MeasureBuilder(dimensionMap)

//        let clusters = getClusters(mb)
//
//        let cache = MyriadCache(dimensions, clusters)
//
//        let context = { 
//                AsOf = DateTimeOffset.UtcNow; 
//                Measures = mb { yield "Environment", "PROD"; yield "Location", "New York"; yield "Application", "Bishop"; yield "Instance", "rex" }
//            }
//
//        let success, value = cache.TryFind("my.property.key", context)

        0
    with
    | ex -> print ConsoleColor.Red 0 (ex.ToString())
            -1


[<EntryPoint>]
let main argv = 
    scriptEntry argv

