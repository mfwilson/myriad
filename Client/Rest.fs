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

    let pathMap = 
        [
            "get",        "get";
            "query",      "query";
            "metadata",   "metadata";
            "dimensions", "list/dimension";

            "put",        "put/property";
        ] |> Map.ofList

    let getRestUri (baseUri : Uri) (key : String) (update : UriBuilder -> Uri) =
        let builder = UriBuilder(baseUri)
        builder.Path <- Path.Combine(builder.Path, pathMap.[key])
        update(builder)
////        client.DownloadString(builder.Uri)
//
//    let query (baseUri : Uri) update = 
//        let json = request baseUri "query" update
//        let response = JsonConvert.DeserializeObject<RestQueryResponse>(json)
//        response.data |> Seq.map (fun m -> m.ToDictionary( (fun p -> p.Key), (fun (p : KeyValuePair<String, String>) -> p.Value) ))

    let getQueryUri (baseUri : Uri) update = 
        getRestUri baseUri "query" update

    let getPutUri (baseUri : Uri) update =
        getRestUri baseUri "put" update