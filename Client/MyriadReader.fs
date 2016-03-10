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
            "getp",       "get/property";
            "query",      "query";
            "metadata",   "metadata";
            "dimensions", "list/dimension";
        ] |> Map.ofList

    let request(key : String) (uriUpdater : UriBuilder -> unit) =
        let builder = UriBuilder(baseUri)
        builder.Path <- Path.Combine(builder.Path, pathMap.[key])
        uriUpdater(builder)
        client.DownloadString(builder.Uri)

    let query uriUpdater = 
        let json = request "query" uriUpdater
        let response = JsonConvert.DeserializeObject<MyriadQueryResponse>(json)
        response.data |> Seq.map (fun m -> m.ToDictionary( (fun p -> p.Key), (fun (p : KeyValuePair<String, String>) -> p.Value) ))

    let get uriUpdater = 
        let json = request "get" uriUpdater
        JsonConvert.DeserializeObject<MyriadGetResponse>(json)        

    interface IDisposable with
        member x.Dispose() = x.Dispose()

    member x.Dispose() = client.Dispose()

    member x.GetDimensionList() =
        let json = request "dimensions" (fun u -> ())
        JsonConvert.DeserializeObject<List<string>>(json)

    member x.GetMetadata() =
        let json = request "metadata" (fun u -> ())  
        JsonConvert.DeserializeObject<List<DimensionValues>>(json)
                        
    member x.GetProperties(propertyNames : String seq) =   
        let update(builder : UriBuilder) =
            if Seq.isEmpty propertyNames then
                ignore()
            else
                let query = HttpUtility.ParseQueryString("")
                query.["property"] <- String.Join(",", propertyNames)
                builder.Query <- query.ToString()

        let json = request "getp" update  
        JsonConvert.DeserializeObject<MyriadGetPropertyResponse>(json)

    member x.QueryProperties(propertyKeys : String seq) =
        let update(builder : UriBuilder) =
            let query = HttpUtility.ParseQueryString("")
            query.["property"] <- String.Join(",", propertyKeys)
            builder.Query <- query.ToString()            
        query update

    member x.Query(dimensionValuesList : List<DimensionValues>) =        
        let uriUpdater(builder : UriBuilder) =
            let property = dimensionValuesList.Find( fun d -> d.Dimension.Name.Equals("Property", StringComparison.InvariantCultureIgnoreCase))
            if property = Unchecked.defaultof<DimensionValues> || property.Values.Length = 0 then 
                ignore()
            else                
                let query = HttpUtility.ParseQueryString("")
                query.["property"] <- String.Join(",", property.Values)
                builder.Query <- query.ToString()
        query uriUpdater

    /// Get values based on a set of measures (query handles returning all possible sets)
    member x.Get(measures : Set<Measure>) =
        let uriUpdater(builder : UriBuilder) =            
            let query = HttpUtility.ParseQueryString("")
            measures |> Set.iter (fun m -> query.[m.Dimension.Name] <- m.Value) 
            builder.Query <- query.ToString()
        get uriUpdater
    
    /// Get values based on a set of measures (query handles returning all possible sets)
    member x.Get(measures : HashSet<Measure>) =                
        x.Get (measures |> Set.ofSeq)
