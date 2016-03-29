open System
open System.Configuration
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

let startTime = DateTimeOffset.UtcNow
let engine = AppConfiguration.getEngine()
let port = AppConfiguration.getPort()
let prefix = AppConfiguration.getPrefix()

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
            path prefix                       >=> setAccessControl >=> RestHandlers.Root startTime
            path (prefix + "/")               >=> setAccessControl >=> RestHandlers.Root startTime
            path (prefix + "/get")            >=> setAccessControl >=> RestHandlers.Get engine 
            path (prefix + "/query")          >=> setAccessControl >=> RestHandlers.Query engine
            path (prefix + "/get/property")   >=> setAccessControl >=> RestHandlers.GetProperty engine 
            path (prefix + "/get/metadata")   >=> setAccessControl >=> RestHandlers.GetMetadata engine 
            path (prefix + "/get/dimensions") >=> setAccessControl >=> RestHandlers.GetDimensions engine
        ]
        PUT >=> choose [
            path (prefix + "/put/property")   >=> RestHandlers.PutProperty engine 
            path (prefix + "/put/measure")    >=> RestHandlers.PutMeasure engine 
            path (prefix + "/put/dimension")  >=> RestHandlers.PutDimension engine 
            path (prefix + "/put/dimensions") >=> RestHandlers.PutDimensionOrder engine 
        ]
    ]

[<EntryPoint>]
let main argv =
    startWebServer serverConfig app
    0 
