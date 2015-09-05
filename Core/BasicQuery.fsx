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

let print (color : ConsoleColor) (depth : int) (text : string) =
    let current = Console.ForegroundColor
    let indent = new String(' ', depth * 4)
    Console.ForegroundColor <- color    
    Console.WriteLine(indent + text)
    Console.ForegroundColor <- current


// Environment -> PROD, UAT, DEV
// Location -> Chicago, New York, London, Amsterdam
// Application -> Rook, Knight, Pawn, Bishop
// Instance -> mary, jimmy, rex
let populateDimension() =
    0


let scriptEntry(args) = 
    try

        

        0
    with
    | ex -> print ConsoleColor.Red 0 (ex.ToString())
            Console.ReadLine() |> ignore
            -1


#if INTERACTIVE
scriptEntry fsi.CommandLineArgs
#endif