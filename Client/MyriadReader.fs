namespace Myriad.Client

open System
open System.Collections.Generic
open System.Collections.Specialized
open System.IO
open System.Linq
open System.Net
open System.Web

open Newtonsoft.Json
open Myriad


type MyriadReader(baseUri : Uri) =
    let client = new WebClient()
    
    let pathMap = 
        [
            "get",        "get";
            "query",      "query";
            "metadata",   "metadata";
            "dimensions", "list/dimension";
        ] |> Map.ofList

    let request(key : String) (update : UriBuilder -> unit) =
        let builder = UriBuilder(baseUri)
        builder.Path <- Path.Combine(builder.Path, pathMap.[key])
        update(builder)
        client.DownloadString(builder.Uri)

    let query update = 
        let json = request "query" update
        let response = JsonConvert.DeserializeObject<RestQueryResponse>(json)
        response.data |> Seq.map (fun m -> m.ToDictionary( (fun p -> p.Key), (fun (p : KeyValuePair<String, String>) -> p.Value) ))

    interface IDisposable with
        member x.Dispose() = x.Dispose()

    member x.Dispose() = client.Dispose()

    member x.GetDimensionList() =
        let json = request "dimensions" (fun u -> ())
        JsonConvert.DeserializeObject<List<string>>(json)

    member x.GetMetadata() =
        let json = request "metadata" (fun u -> ())  
        JsonConvert.DeserializeObject<List<DimensionValues>>(json)
                        
    member x.QueryProperties(propertyKeys : String seq) =
        let update(builder : UriBuilder) =
            let query = HttpUtility.ParseQueryString("")
            query.["property"] <- String.Join(",", propertyKeys)
            builder.Query <- query.ToString()            
        query update

    member x.Query(dimensionValuesList : List<DimensionValues>) =
        
        let update(builder : UriBuilder) =
            let property = dimensionValuesList.Find( fun d -> d.Dimension.Name.Equals("Property", StringComparison.InvariantCultureIgnoreCase))
            if property = Unchecked.defaultof<DimensionValues> || property.Values.Length = 0 then 
                ignore()
            else                
                let query = HttpUtility.ParseQueryString("")
                query.["property"] <- String.Join(",", property.Values)
                builder.Query <- query.ToString()
        query update

//        let json = request "query" update
//        let response = JsonConvert.DeserializeObject<RestResponse>(json)
//        response.data |> Seq.map (fun m -> m.ToDictionary( (fun p -> p.Key), (fun (p : KeyValuePair<String, String>) -> p.Value) ))
        
