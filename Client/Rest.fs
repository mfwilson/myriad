namespace Myriad.Client

open System
open System.Collections.Generic
open System.Collections.Specialized
open System.IO
open System.Linq
open System.Net
open System.Web

open Newtonsoft.Json

type RestQueryResponse =
    { data : Map<String, String> seq }


module Rest =

    let pathMap = 
        [
            "get",        "get";
            "query",      "query";
            "metadata",   "metadata";
            "dimensions", "list/dimension";
        ] |> Map.ofList


//    let request (baseUri : Uri) (key : String) (update : UriBuilder -> Uri) =
//        let builder = UriBuilder(baseUri)
//        builder.Path <- Path.Combine(builder.Path, pathMap.[key])
//        update(builder)
////        client.DownloadString(builder.Uri)
//
//    let query (baseUri : Uri) update = 
//        let json = request baseUri "query" update
//        let response = JsonConvert.DeserializeObject<RestQueryResponse>(json)
//        response.data |> Seq.map (fun m -> m.ToDictionary( (fun p -> p.Key), (fun (p : KeyValuePair<String, String>) -> p.Value) ))

