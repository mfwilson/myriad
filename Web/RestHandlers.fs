﻿namespace Myriad.Web

open System
open System.Collections.Specialized
open System.Globalization
open System.Net
open System.Text
open System.Web

open NLog 

open Suave
open Suave.Http
open Suave.RequestErrors
open Suave.Successful

open Newtonsoft.Json

open Myriad

module RestHandlers =
    let logger = LogManager.GetCurrentClassLogger()

    let private fromJson<'a> json =
        match json with
        | json when String.IsNullOrEmpty(json) -> None
        | _ -> Some(JsonConvert.DeserializeObject(json, typeof<'a>) :?> 'a)

    let private fromRequest<'a> (req : HttpRequest) = 
        let getString rawForm = Encoding.UTF8.GetString(rawForm)
        req.rawForm |> getString |> fromJson<'a>

    let private getAsOf (kv : NameValueCollection) =
        let value = kv.["asOf"]     // Compare is case-insensitive
        if value <> null then            
            let success, result = DateTimeOffset.TryParse(value, null, DateTimeStyles.AssumeUniversal)
            if success then result else DateTimeOffset.UtcNow
        else
            DateTimeOffset.UtcNow

    let private getContext (getDimension : String -> Dimension option) (kv : NameValueCollection) =
        let getMeasure(key) = 
            let dimension = getDimension(key)
            match dimension with
            | None -> None
            | Some(d) when d.Name = "Property" -> None
            | Some(d) -> Some( { Dimension = dimension.Value; Value = kv.[key] } )

        let measures = kv.AllKeys |> Seq.choose getMeasure |> Set.ofSeq
        { AsOf = getAsOf kv; Measures = measures }

    let private getResponseString<'T> (format : String) (response : 'T) =
        match format with
        | f when String.IsNullOrEmpty(format) || format.ToLower() = "json" -> "text/json", JsonConvert.SerializeObject(response)
        | f when format.ToLower() = "xml" -> "text/xml", XmlConvert.SerializeObject(response)
        | f when format.ToLower() = "text" -> "text/raw", response.ToString()
        | _ -> raise(ArgumentException("Unknown format [" + format + "]"))

    let private getPropertyKeys(kv : NameValueCollection) =
        match kv.["property"] with
        | p when not(String.IsNullOrEmpty(p)) -> p.Split([|','|]) 
        | _ -> [| "" |]

    let handleRequest (engine : MyriadEngine) (x : HttpContext) (handler : NameValueCollection -> (String -> WebPart) * String * String) =
        async { 
            let requestId = Guid.NewGuid()
            try
                logger.Info("RECV: [{0}] [{1}]", requestId, x.request.url)
                let kv = HttpUtility.ParseQueryString(x.request.rawQuery)
                let webResponse, contentType, message = handler(kv)
                let! ctx = Writers.setMimeType contentType x                                
                logger.Info("SEND: [{0}] [{1}] {2} Length: {3}", requestId, x.request.url, contentType, message.Length)
                logger.Debug("SEND: [{0}] [{1}]", requestId, message)                                
                return! webResponse message ctx.Value
            with 
            | :? ArgumentException as ex -> 
                logger.Error("UNPROCESSABLE_ENTITY: [{0}] [{1}] {2}\r\n{3}", requestId, x.request.url, x.request.rawQuery, ex.ToString())
                let! ctx = Writers.setMimeType "text/plain" x
                return! UNPROCESSABLE_ENTITY (ex.Message) ctx.Value
            | ex -> 
                logger.Error("BAD_REQUEST: [{0}] [{1}] {2}\r\n{3}", requestId, x.request.url, x.request.rawQuery, ex.ToString())
                let! ctx = Writers.setMimeType "text/plain" x
                return! BAD_REQUEST (ex.Message) ctx.Value
        }        
        
    /// Provides an ordered list of dimensions 
    let GetDimensions (engine : MyriadEngine) (x : HttpContext) = 
        let getDimensions (kv : NameValueCollection) =            
            let response = engine.GetDimensions() |> List.map (fun d -> d.Name)
            let contentType, message = getResponseString kv.["format"] response
            OK, contentType, message
        handleRequest engine x getDimensions

    /// Provides a view over both properties and dimensions
    let GetMetadata (engine : MyriadEngine) (x : HttpContext) = 
        let getMetadata (kv : NameValueCollection) =            
            let response = engine.GetMetadata()
            let contentType, message = getResponseString kv.["format"] response
            OK, contentType, message
        handleRequest engine x getMetadata

    /// Query -> JSON w/ context
    let Query (engine : MyriadEngine) (x : HttpContext) =
        let query (kv : NameValueCollection) =
            let context = getContext (engine.GetDimension) kv
            let properties = getPropertyKeys(kv)
                             |> Seq.map (fun p -> engine.Query(p, context))
                             |> Seq.concat            
            let dimensions = engine.GetDimensions() 
            let dataRows = properties |> Seq.mapi (fun i p -> Cluster.ToMap(fst(p).Key, snd(p), dimensions, i))

            let response = { data = dataRows }            
            let contentType, message = getResponseString kv.["format"] response
            OK, contentType, message
        handleRequest engine x query

    /// GET -> URL properties with dimensions name=value
    let Get (engine : MyriadEngine) (x : HttpContext) =
        let get (kv : NameValueCollection) =
            let kv = HttpUtility.ParseQueryString(x.request.rawQuery)
            let context = getContext (engine.GetDimension) kv

            let properties = getPropertyKeys(kv)
                             |> Seq.map (fun p -> engine.Get(p, context))
                             |> Seq.concat

            let response = { MyriadGetResponse.Requested = DateTimeOffset.UtcNow; Context = context; Properties = properties }            
            let contentType, message = getResponseString kv.["format"] response
            OK, contentType, message
        handleRequest engine x get

    let GetProperty (engine : MyriadEngine) (x : HttpContext) =
        let getProperty (kv : NameValueCollection) =
            let asOf = getAsOf kv
            let properties = getPropertyKeys(kv) |> Seq.choose (fun p -> engine.Get(p, asOf))
            let response = { Requested = DateTimeOffset.UtcNow; Properties = properties }
            let contentType, message = getResponseString kv.["format"] response       
            OK, contentType, message
        handleRequest engine x getProperty
        
    /// PUT property operation (JSON data) -> property
    let PutProperty (engine : MyriadEngine) (x : HttpContext) =
        let putProperty (kv : NameValueCollection) =
            let property = fromRequest<PropertyOperation>(x.request)
            if property.IsNone then
                let contentType, message = getResponseString "text" "PropertyOperation could not be read."
                BAD_REQUEST, contentType, message
            else
                let newProperty = engine.Put(property.Value)
                let response = { Requested = DateTimeOffset.UtcNow; Property = newProperty }
                let contentType, message = getResponseString kv.["format"] response       
                OK, contentType, message
        handleRequest engine x putProperty                                

    /// PUT new dimension+value (measure) -> Dimension * string list
    let PutMeasure (engine : MyriadEngine) (x : HttpContext) =
        let putMeasure (kv : NameValueCollection) =
            let ``measure`` = fromRequest<Measure>(x.request)
            if ``measure``.IsNone then
                BAD_REQUEST, "text", "Measure could not be read."
            else
                logger.Info("Adding measure [{0}]", ``measure``.Value.ToString())
                let response = engine.AddMeasure(``measure``.Value)
                if response.IsNone then
                    BAD_REQUEST, "text", "Measure could not be added."
                else
                    let contentType, message = getResponseString kv.["format"] response.Value                    
                    OK, contentType, message         
        handleRequest engine x putMeasure
