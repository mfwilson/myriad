module BasicQuery

#if INTERACTIVE
#r "System.Xml.dll" 
#r "System.Xml.Linq.dll"
#r "bin/Debug/Myriad.Core.dll"
#endif

open System
open System.IO
open System.Diagnostics

open Myriad

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

//let print (color : System.ConsoleColor) (depth : int) (text : string) =
//    let current = Console.ForegroundColor
//    let indent = new String(' ', depth * 4)
//    Console.ForegroundColor <- color    
//    Console.WriteLine(indent + text)
//    Console.ForegroundColor <- current


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

let getClusters(map : Map<String, Dimension>) =
    
    let now = DateTimeOffset.UtcNow
    let utcTicks = now.UtcTicks
    //let c = Cluster(0, now.UtcTicks, { Property.Id = 0; Name = "" }, "apple", Set.ofList [])
    
    let property = { Property.Id = 0; Name = "my.property.key" }

    Seq.ofList
        [
            Cluster(0, utcTicks, property, "apple", Set.ofList [Measure(map.["Environment"], "PROD")] )
        ]



let scriptEntry(args) = 
    try
        let dimensions = getDimensions()
        let dimensionMap = dimensions |> Seq.map (fun d -> d.Name, d) |> Map.ofSeq
        
        let clusters = getClusters(dimensionMap)

        let cache = DimensionCache(dimensions, clusters)

        0
    with
    | ex -> //printfn "%s" (ex.ToString())            
            -1


#if INTERACTIVE
scriptEntry fsi.CommandLineArgs
#endif