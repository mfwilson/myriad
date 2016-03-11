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

type MyriadWriter(baseUri : Uri) =

    let client = new WebClient()
        
    interface IDisposable with
        member x.Dispose() = x.Dispose()

    member x.Dispose() = 
        client.Dispose()
    
    member x.PutProperty(propertyOperation : PropertyOperation) =
        let uri = Rest.getPutPropertyUri baseUri (fun builder -> builder.Uri)
        let request = JsonConvert.SerializeObject(propertyOperation)        
        let response = client.UploadString(uri, "PUT", request)
        JsonConvert.DeserializeObject<MyriadPutPropertyResponse>(response)

    member x.AddDimension(dimensionName : String) =
        let uriUpdater(builder : UriBuilder) =
            let query = HttpUtility.ParseQueryString("")
            query.["dimension"] <- dimensionName
            builder.Query <- query.ToString()     
            builder.Uri       
        let uri = Rest.getPutDimensionUri baseUri uriUpdater
        let response = client.UploadString(uri, "PUT", "")
        JsonConvert.DeserializeObject<Dimension>(response)

    member x.AddMeasure(``measure`` : Measure) =
        let uri = Rest.getPutMeasureUri baseUri (fun builder -> builder.Uri)        
        let request = JsonConvert.SerializeObject(``measure``)
        let response = client.UploadString(uri, "PUT", request)
        JsonConvert.DeserializeObject<DimensionValues>(response)

    member x.AddMeasure(dimension : Dimension, value : String) =
        x.AddMeasure({ Dimension = dimension; Value = value})
        
