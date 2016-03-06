namespace Myriad.Web

open System
open System.Collections.Specialized
open System.Globalization
open System.Net
open System.Text
open System.Web

open Suave
open Suave.Http
open Suave.RequestErrors
open Suave.Successful

open Newtonsoft.Json

open Myriad

type AjaxResponse =
    { data : Map<String, String> seq }

module RestHandlers =

    let private fromJson<'a> json =
        JsonConvert.DeserializeObject(json, typeof<'a>) :?> 'a    

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
        | _ -> raise(ArgumentException("Unknown format [" + format + "]"))

    let private getPropertyKeys(kv : NameValueCollection) =
        match kv.["property"] with
        | p when not(String.IsNullOrEmpty(p)) -> p.Split([|','|]) 
        | _ -> [| "" |]

    /// Provides an ordered list of dimensions 
    let DimensionList (engine : MyriadEngine) (x : HttpContext) = 
        async {
            let dimensionList = engine.GetDimensions() |> List.map (fun d -> d.Name)
            let message = JsonConvert.SerializeObject(dimensionList)
            return! OK message x
        }

    /// Provides a view over both properties and dimensions
    let Metadata (engine : MyriadEngine) (x : HttpContext) = 
        async {
            let metadata = engine.GetMetadata()
            let message = JsonConvert.SerializeObject(metadata)
            return! OK message x
        }

    /// Query -> JSON w/ context
    let Query (engine : MyriadEngine) (x : HttpContext) =
        async {
            Console.WriteLine("REQ: Query " + x.request.rawQuery)

            let kv = HttpUtility.ParseQueryString(x.request.rawQuery)

            let context = getContext (engine.GetDimension) kv

            let measuresAsString = context.Measures |> Seq.map (fun m -> m.ToString())
            Console.WriteLine("Measures: " + String.Join(", ", measuresAsString))

            let properties = getPropertyKeys(kv)
                             |> Seq.map (fun p -> engine.Query(p, context))
                             |> Seq.concat
            
            let dimensions = engine.GetDimensions() 
            let dataRows = properties |> Seq.mapi (fun i p -> Cluster.ToMap(fst(p).Key, snd(p), dimensions, i))

            let response = { data = dataRows }
            let message = JsonConvert.SerializeObject(response)
            Console.WriteLine("Found {0} clusters\r\n{1}", Seq.length properties, message)
            return! OK message x
        }    

    /// GET -> URL properties with dimensions name=value
    let Get (engine : MyriadEngine) (x : HttpContext) =
        async {
            try
                Console.WriteLine("REQ: Get " + x.request.rawQuery)

                let kv = HttpUtility.ParseQueryString(x.request.rawQuery)
                let context = getContext (engine.GetDimension) kv

                let properties = getPropertyKeys(kv)
                                 |> Seq.map (fun p -> engine.Get(p, context))
                                 |> Seq.concat

                let response = { MyriadQueryResponse.Requested = DateTimeOffset.UtcNow; Context = context; Properties = properties }            
                let contentType, message = getResponseString kv.["format"] response       
                                
                let! ctx = Writers.setMimeType contentType x
                return! OK message ctx.Value 
            with 
            | :? ArgumentException as ex -> 
                Console.WriteLine("UNPROCESSABLE_ENTITY: Get {0}\r\n{1}", x.request.rawQuery, ex.ToString())
                let! ctx = Writers.setMimeType "text/plain" x
                return! UNPROCESSABLE_ENTITY (ex.Message) ctx.Value
            | ex -> 
                Console.WriteLine("BAD_REQUEST: Get {0}\r\n{1}", x.request.rawQuery, ex.ToString())
                let! ctx = Writers.setMimeType "text/plain" x
                return! BAD_REQUEST (ex.Message) ctx.Value
        }        

    let GetProperty (engine : MyriadEngine) (x : HttpContext) =
        async {
            try
                Console.WriteLine("REQ: GetProperty " + x.request.rawQuery)

                let kv = HttpUtility.ParseQueryString(x.request.rawQuery)

                let asOf = getAsOf kv

                let properties = getPropertyKeys(kv)
                                 |> Seq.choose (fun p -> engine.Get(p, asOf))

                let response = { Requested = DateTimeOffset.UtcNow; Properties = properties }
                let contentType, message = getResponseString kv.["format"] response       

                let! ctx = Writers.setMimeType contentType x
                return! OK message ctx.Value 
            with 
            | :? ArgumentException as ex -> 
                Console.WriteLine("UNPROCESSABLE_ENTITY: Get {0}\r\n{1}", x.request.rawQuery, ex.ToString())
                let! ctx = Writers.setMimeType "text/plain" x
                return! UNPROCESSABLE_ENTITY (ex.Message) ctx.Value
            | ex -> 
                Console.WriteLine("BAD_REQUEST: Get {0}\r\n{1}", x.request.rawQuery, ex.ToString())
                let! ctx = Writers.setMimeType "text/plain" x
                return! BAD_REQUEST (ex.Message) ctx.Value
        }        
        
    /// PUT property operation (JSON data) -> property
    let PutProperty (engine : MyriadEngine) (x : HttpContext) =
        async {            
            try
                Console.WriteLine("REQ: put property " + x.request.rawQuery)                

                let kv = HttpUtility.ParseQueryString(x.request.rawQuery)                
                let property = fromRequest<PropertyOperation>(x.request)
                let newProperty = engine.Put(property)
                let response = { Requested = DateTimeOffset.UtcNow; Property = newProperty }
                let contentType, message = getResponseString kv.["format"] response       
                                
                let! ctx = Writers.setMimeType contentType x
                return! OK message ctx.Value
            with 
            | :? ArgumentException as ex -> 
                Console.WriteLine("UNPROCESSABLE_ENTITY: Get {0}\r\n{1}", x.request.rawQuery, ex.ToString())
                let! ctx = Writers.setMimeType "text/plain" x
                return! UNPROCESSABLE_ENTITY (ex.Message) ctx.Value
            | ex -> 
                Console.WriteLine("BAD_REQUEST: Get {0}\r\n{1}", x.request.rawQuery, ex.ToString())
                let! ctx = Writers.setMimeType "text/plain" x
                return! BAD_REQUEST (ex.Message) ctx.Value
        }

    /// PUT new dimension+value (measure) -> Dimension * string list
    let PutMeasure (engine : MyriadEngine) (x : HttpContext) =
        async {            
            try
                Console.WriteLine("REQ: put measure " + x.request.rawQuery)                

//                let kv = HttpUtility.ParseQueryString(x.request.rawQuery)                
//                let property = fromRequest<PropertyOperation>(x.request)
//                let newProperty = engine.Put(property)
//                let response = { Requested = DateTimeOffset.UtcNow; Property = newProperty }
//                let contentType, message = getResponseString kv.["format"] response       
                                
                //let! ctx = Writers.setMimeType contentType x
                return! OK "Not implemented" x
            with 
            | :? ArgumentException as ex -> 
                Console.WriteLine("UNPROCESSABLE_ENTITY: Get {0}\r\n{1}", x.request.rawQuery, ex.ToString())
                let! ctx = Writers.setMimeType "text/plain" x
                return! UNPROCESSABLE_ENTITY (ex.Message) ctx.Value
            | ex -> 
                Console.WriteLine("BAD_REQUEST: Get {0}\r\n{1}", x.request.rawQuery, ex.ToString())
                let! ctx = Writers.setMimeType "text/plain" x
                return! BAD_REQUEST (ex.Message) ctx.Value
        }