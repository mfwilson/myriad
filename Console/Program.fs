module Myriad.Test

open System
open System.IO
open System.Diagnostics

open Newtonsoft.Json
open Newtonsoft.Json.Serialization

open Myriad
open Myriad.Client
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
    

let getClusters(mb : MeasureBuilder) =
    Seq.ofList
        [
            Cluster.Create("apple", mb { yield "Environment", "PROD"; yield "Location", "London"; yield "Instance", "rex" } )
            Cluster.Create("pear", mb { yield "Environment", "PROD"; yield "Instance", "rex" } )
            Cluster.Create("pecan", mb { yield "Instance", "rex" } )
            Cluster.Create("peach", mb { yield "Application", "Rook" } )
            Cluster.Create("strawberry", mb { yield "Location", "Chicago" } )
            Cluster.Create("apricot", mb { yield "Environment", "DEV" } )
            Cluster.Create("pumpkin", Set.empty )
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
        //let reader = new MyriadReader(Uri("http://localhost:7888/api/1/myriad"))

        //let m = reader.GetMetadata()

        
        let engine = MyriadEngine(MockStore())

        let mb = engine.MeasureBuilder
        let pb = engine.PropertyBuilder


        let cluster = Cluster.Create("apple", mb { yield "Environment", "PROD"; yield "Location", "London"; yield "Instance", "rex" } )
        
        let prop = pb.Create "something" Epoch.UtcNow [ cluster ]
        
        let propJson = JsonConvert.SerializeObject(prop)
        let propOb = JsonConvert.DeserializeObject<Property>(propJson)


        // { Value = cluster.Value; Measures = cluster.Measures; UserName = cluster.UserName }
        let request = ClusterOperation.Add("mm.aa.bb", cluster )

        let jsonRequest = JsonConvert.SerializeObject(request)

        let roundtrip = JsonConvert.DeserializeObject<ClusterOperation>(jsonRequest)


        //let setBuilder = new PropertyBuilder(dimensions)

        let properties = pb.Create "my.property.key" Epoch.UtcNow (getClusters mb)

        let cache = new MyriadCache()
        cache.Insert(properties)

        let context = { 
                AsOf = DateTimeOffset.UtcNow; 
                Measures = mb { yield "Environment", "PROD"; yield "Location", "New York"; yield "Application", "Bishop"; yield "Instance", "rex" }
            }

        let success, value = cache.TryFind("my.property.key", context)


        let emptyContext = { AsOf = DateTimeOffset.UtcNow; Measures = Set.empty }
        let resultsAny = cache.GetAny(emptyContext)

        let filterContext = { AsOf = DateTimeOffset.UtcNow; Measures = mb { yield "Environment", "PROD" } }
        let resultsFilter = cache.GetAny(filterContext) |> Seq.toList


        //let m = Cluster.ToMap("my.property.key", snd(resultsFilter.[0]), dimensions, 0)

        //////

        //let redis = new RedisConnection()

        //let dimensions = redis.GetDimensions()

        //redis.AddDimensionValues(dimensions.[0], [ "PROD"; "UAT"; "DEV" ] )
        
        //let g = redis.GetDimensionValues(dimensions)

        //let d1 = redis.CreateDimension("Environment")
        

//        let dimensions = getDimensions()
//        let dimensionMap = dimensions |> Seq.map (fun d -> d.Name, d) |> Map.ofSeq
//        let mb = new MeasureBuilder(dimensionMap)

//        let clusters = getClusters(mb)
//
//        let cache = MyriadCache(dimensions, clusters)
//

        0
    with
    | ex -> print ConsoleColor.Red 0 (ex.ToString())
            -1


[<EntryPoint>]
let main argv = 
    scriptEntry argv

