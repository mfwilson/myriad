﻿open System
open System.Net

open Suave
open Suave.Operators
open Suave.EventSource
open Suave.Filters
open Suave.Writers
open Suave.Successful

open Myriad
open Myriad.Store
open Myriad.Web

let store = new MemoryStore()
[ "Customer"; "Environment"; "Application"; "Instance" ] |> List.iter (fun d -> store.AddDimension(d) |> ignore)
let engine = new MyriadEngine(store)

let port = Sockets.Port.Parse("7888")
let prefix = "/api/1/myriad/"

let serverConfig = 
    { defaultConfig with
       bindings = [ HttpBinding.mk HTTP IPAddress.Any port ]
       logger = LoggingAdapter()
    }

let setAccessControl =
    Writers.setHeader "Access-Control-Allow-Origin" "*" 
    >=> Writers.setHeader "Access-Control-Allow-Headers" "Origin, X-Requested-With, Content-Type, Accept, Key"

let app : WebPart =
    choose [
        GET >=> choose [ 
            path (prefix + "get") >=> setAccessControl >=> RestHandlers.Get engine 
            path (prefix + "query") >=> setAccessControl >=> Writers.setMimeType("text/json") >=> RestHandlers.Query engine
            path (prefix + "get/property") >=> setAccessControl >=> RestHandlers.GetProperty engine 
            path (prefix + "get/metadata") >=> Writers.setMimeType("text/json") >=> RestHandlers.GetMetadata engine 
            path (prefix + "get/dimensions") >=> Writers.setMimeType("text/json") >=> RestHandlers.GetDimensions engine
        ]
        PUT >=> choose [
            path (prefix + "put/property") >=> RestHandlers.PutProperty engine 
            path (prefix + "put/measure") >=> RestHandlers.PutMeasure engine 
            path (prefix + "put/dimension") >=> RestHandlers.PutDimension engine 
            path (prefix + "put/dimensions") >=> RestHandlers.PutDimensionOrder engine 
        ]
    ]

[<EntryPoint>]
let main argv =
    startWebServer serverConfig app
    0 
