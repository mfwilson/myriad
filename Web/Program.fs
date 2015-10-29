
open System
open System.Net

open Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Types
open Suave.Web

open WebSharper.Suave

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
            GET >>= path "/" >>= (WebSharperAdapter.ToWebPart MyriadSite.MainPage)
            path "/find" >>= RestHandlers.Find cache store
            path "/query" >>= RestHandlers.Query cache store
            pathStarts "/dimensions" >>= RestHandlers.Dimensions store    
        ])
    0 
