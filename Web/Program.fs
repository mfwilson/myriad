open System
open System.Configuration
open System.Net
open System.Text.RegularExpressions

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

let modifyRequest = 
    RestHandlers.setAccessControl >=> RestHandlers.setCustomHeaders

let app : WebPart =
    choose [
        GET >=> choose [ 
            path prefix                       >=> modifyRequest >=> RestHandlers.Root startTime
            path (prefix + "/")               >=> modifyRequest >=> RestHandlers.Root startTime
            path (prefix + "/get")            >=> modifyRequest >=> RestHandlers.Get engine 
            path (prefix + "/query")          >=> modifyRequest >=> RestHandlers.Query engine
            path (prefix + "/get/property")   >=> modifyRequest >=> RestHandlers.GetProperty engine 
            path (prefix + "/get/metadata")   >=> modifyRequest >=> RestHandlers.GetMetadata engine 
            path (prefix + "/get/dimensions") >=> modifyRequest >=> RestHandlers.GetDimensions engine
        ]
        PUT >=> choose [
            path (prefix + "/put/property")   >=> modifyRequest >=> RestHandlers.PutProperty engine 
            path (prefix + "/put/measure")    >=> modifyRequest >=> RestHandlers.PutMeasure engine 
            path (prefix + "/put/dimension")  >=> modifyRequest >=> RestHandlers.PutDimension engine 
            path (prefix + "/put/dimensions") >=> modifyRequest >=> RestHandlers.PutDimensionOrder engine 
        ]
    ]

[<EntryPoint>]
let main argv =
    startWebServer serverConfig app
    0 
