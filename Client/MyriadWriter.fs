﻿namespace Myriad.Client

open System
open System.Collections.Generic
open System.Collections.Specialized
open System.IO
open System.Linq
open System.Net
open System.Web

open Newtonsoft.Json
open Myriad

type MyriadWriter(baseUri : Uri) =

    let client = new WebClient()
        
    interface IDisposable with
        member x.Dispose() = x.Dispose()

    member x.Dispose() = 
        client.Dispose()
    
    member x.PutProperty(property : PropertyOperation) =
        let uri = Rest.getPutUri baseUri (fun builder -> builder.Uri)        
        let json = client.DownloadString(uri)
        JsonConvert.DeserializeObject<Property>(json)

    member x.AddDimensionValue(dimension : Dimension, value : String) =

        ignore()
