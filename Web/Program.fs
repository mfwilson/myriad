open System
open System.Net

open Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Types
open Suave.Web

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
    >>= Writers.setHeader "Access-Control-Allow-Headers" "Origin, X-Requested-With, Content-Type, Accept, Key"

[<EntryPoint>]
let main argv =
    startWebServer 
        serverConfig 
        (choose [
            path "/api/1/find" >>= setAccessControl  >>= RestHandlers.Find engine
            path "/api/1/query" >>= setAccessControl >>= RestHandlers.Query engine
            path "/api/1/dimensions/list" >>= RestHandlers.DimensionList engine
            path "/api/1/metadata" >>= RestHandlers.Metadata engine
        ])
    0 
