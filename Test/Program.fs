module Myriad.Test

open System
open System.IO
open System.Diagnostics

open System.Runtime.Serialization.Json

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
    [ { Dimension.Id = 32; Name = "Environment" };
      { Dimension.Id = 21; Name = "Location" };
      { Dimension.Id = 44; Name = "Application" };
      { Dimension.Id = 98; Name = "Instance" }
    ]   

let getClusters(mb : MeasureBuilder) =    
    let now = DateTimeOffset.UtcNow
    let utcTicks = now.UtcTicks
    let property = { Property.Id = 0; Name = "my.property.key" }

    Seq.ofList
        [                                           
            Cluster(0, utcTicks, property, "apple", mb { yield "Environment", "PROD"; yield "Location", "London"; yield "Instance", "rex" } )
            Cluster(0, utcTicks, property, "pear", mb { yield "Environment", "PROD"; yield "Instance", "rex" } )
            Cluster(0, utcTicks, property, "pecan", mb { yield "Instance", "rex" } )
            Cluster(0, utcTicks, property, "peach", mb { yield "Application", "Rook" } )
            Cluster(0, utcTicks, property, "strawberry", mb { yield "Location", "Chicago" } )
            Cluster(0, utcTicks, property, "apricot", mb { yield "Environment", "DEV" } )
            Cluster(0, utcTicks, property, "pumpkin", Set.empty )
        ]

let scriptEntry(args) = 
    try
        let redis = new RedisConnection()
        let g = redis.GetDimensions()

        let serializer = new DataContractJsonSerializer(typeof<Dimension>)
        //serializer.Se

        let dimensions = getDimensions()
        let dimensionMap = dimensions |> Seq.map (fun d -> d.Name, d) |> Map.ofSeq
        let mb = new MeasureBuilder(dimensionMap)

        let clusters = getClusters(mb)

        let cache = MyriadCache(dimensions, clusters)        

        let context = { 
                AsOf = DateTimeOffset.UtcNow; 
                Measures = mb { yield "Environment", "PROD"; yield "Location", "New York"; yield "Application", "Bishop"; yield "Instance", "rex" }
            }

        let success, value = cache.TryFind("my.property.key", context)

        0
    with
    | ex -> print ConsoleColor.Red 0 (ex.ToString())            
            -1


[<EntryPoint>]
let main argv = 
    scriptEntry argv

