open System
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

let engine = new MyriadEngine(new MockStore())

let port = Sockets.Port.Parse("7888")
let prefix = "/api/1/myriad/"

let serverConfig = 
    { defaultConfig with
       bindings = [ HttpBinding.mk HTTP IPAddress.Any port ]
    }

let setAccessControl =
    Writers.setHeader "Access-Control-Allow-Origin" "*" 
    >=> Writers.setHeader "Access-Control-Allow-Headers" "Origin, X-Requested-With, Content-Type, Accept, Key"

let app : WebPart =
    choose [
        GET >=> choose [ 
            path (prefix + "get") >=> setAccessControl >=> Writers.setMimeType("text/json") >=> RestHandlers.Get engine 
            path (prefix + "query") >=> setAccessControl >=> Writers.setMimeType("text/json") >=> RestHandlers.Query engine
            path (prefix + "dimensions/list") >=> Writers.setMimeType("text/json") >=> RestHandlers.DimensionList engine
            path (prefix + "metadata") >=> Writers.setMimeType("text/json") >=> RestHandlers.Metadata engine 
        ]
        PUT >=> choose [
            path (prefix + "set") >=> Writers.setMimeType("text/json") >=> RestHandlers.Set engine 
        ]
    ]

[<EntryPoint>]
let main argv =
    startWebServer serverConfig app
    0 
