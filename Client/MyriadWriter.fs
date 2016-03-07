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
        let uri = Rest.getPutUri baseUri (fun builder -> builder.Uri)        
        let request = JsonConvert.SerializeObject(propertyOperation)        
        let response = client.UploadString(uri, "PUT", request)
        JsonConvert.DeserializeObject<Property>(response)

    member x.AddDimensionValue(dimension : Dimension, value : String) =

        ignore()
