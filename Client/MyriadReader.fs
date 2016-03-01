namespace Myriad.Client

open System
open System.Collections.Generic
open System.IO
open System.Net

open Newtonsoft.Json
open Myriad

type MyriadReader(baseUri : Uri) =

    let client = new WebClient()
    
    let pathMap = 
        [
            "get",        "get";
            "query",      "query";
            "metadata",   "metadata";
            "dimensions", "dimension/list";
        ] |> Map.ofList

    let request(key : String) =
        let builder = UriBuilder(baseUri)
        builder.Path <- Path.Combine(builder.Path, pathMap.[key])
        client.DownloadString(builder.Uri)

    interface IDisposable with
        member x.Dispose() = x.Dispose()

    member x.Dispose() = client.Dispose()


    member x.GetMetadata() =
        let json = request("metadata")    
        JsonConvert.DeserializeObject<List<DimensionValues>>(json)
                        

(*
    var restServerUrl = "http://mattpc:7888";
    var client = new WebClient();
    var json = client.DownloadString(restServerUrl + "/api/1/metadata");
    var metadata = Json.Decode(json);

    var dimensionsJson = client.DownloadString(restServerUrl + "/api/1/dimensions/list");
    var dimensions = Json.Decode<List<string>>(dimensionsJson);

    var columnList = new List<string>(dimensions);
    columnList.Insert(0, "Ordinal");
    columnList.AddRange( new [] { "Property", "Value" } );
*)

