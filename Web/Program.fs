
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

let store = new MockStore()

let cache = new MyriadCache()
store.SampleProperties |> Seq.iter cache.Insert

let port = Sockets.Port.Parse("8083")

let serverConfig = 
    { defaultConfig with
       bindings = [ HttpBinding.mk HTTP IPAddress.Any port ]
    }

[<EntryPoint>]
let main argv =
    startWebServer 
        serverConfig 
        (choose [
            path "/find" 
                >>= RestHandlers.Find cache store
                >>= Writers.setHeader "Access-Control-Allow-Origin" "*" 
                >>= Writers.setHeader "Access-Control-Allow-Headers" "Origin, X-Requested-With, Content-Type, Accept, Key"

            path "/query" 
                >>= RestHandlers.Query cache store
                >>= Writers.setHeader "Access-Control-Allow-Origin" "*" 
                >>= Writers.setHeader "Access-Control-Allow-Headers" "Origin, X-Requested-With, Content-Type, Accept, Key"

            path "/properties" >>= RestHandlers.Properties cache store
            pathStarts "/dimensions" >>= RestHandlers.Dimensions store
            pathStarts "/metadata" >>= RestHandlers.Metadata store
            
            path "/api/1/dimensions/list" >>= RestHandlers.DimensionList store
            // pathStarts "/api/1" >>= merge 1..N cluster sets (POST)
        ])
    0 
