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
            path "/api/1/get" >=> setAccessControl >=> Writers.setMimeType("text/json") >=> RestHandlers.Get engine 
            path "/api/1/query" >=> setAccessControl >=> RestHandlers.Query engine
            path "/api/1/dimensions/list" >=> RestHandlers.DimensionList engine
            path "/api/1/metadata" >=> RestHandlers.Metadata engine 
        ]
    ]

[<EntryPoint>]
let main argv =
    startWebServer serverConfig app
    0 
