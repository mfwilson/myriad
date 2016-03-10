namespace Myriad.Client

open System
open System.Collections.Generic
open System.Collections.Specialized
open System.IO
open System.Linq
open System.Net
open System.Web

open Newtonsoft.Json

module Rest =

    let getRestUri (baseUri : Uri) (path : String) (uriUpdater : UriBuilder -> Uri) =
        let builder = UriBuilder(baseUri)
        builder.Path <- Path.Combine(builder.Path, path)
        uriUpdater(builder)

//    let getQueryUri (baseUri : Uri) update = 
//        getRestUri baseUri "query" update

    let getPutPropertyUri (baseUri : Uri) update =
        getRestUri baseUri "put/property" update

    let getPutMeasureUri (baseUri : Uri) update =
        getRestUri baseUri "put/measure" update