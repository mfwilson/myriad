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

    let private getAsOf (kv : NameValueCollection) =
        let value = kv.["asOf"]     // Compare is case-insensitive
        if value <> null then            
            let success, result = DateTimeOffset.TryParse(value, null, DateTimeStyles.AssumeUniversal)
            if success then result else DateTimeOffset.UtcNow
        else
            DateTimeOffset.UtcNow

    let private getContext (getDimension : String -> IDimension option) (kv : NameValueCollection) =
        let getMeasure(key) = 
            let dimension = getDimension(key)
            if dimension.IsNone then None else Some( { Dimension = dimension.Value; Value = kv.[key] } )

        let measures = kv.AllKeys |> Seq.choose getMeasure |> Set.ofSeq
        { AsOf = getAsOf kv; Measures = measures }

    let private getResponseString<'T> (format : String) (response : 'T) =
        match format with
        | f when String.IsNullOrEmpty(format) || format.ToLower() = "json" -> "text/json", JsonConvert.SerializeObject(response)
        | f when format.ToLower() = "xml" -> "text/xml", XmlConvert.SerializeObject(response)
        | _ -> raise(ArgumentException("Unknown format [" + format + "]"))

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

            let properties = engine.Query(kv.["property"], context)
            let dimensions = engine.GetDimensions() 
            let dataRows = properties |> Seq.mapi (fun i p -> Cluster.ToMap(fst(p).Key, snd(p), dimensions, i))

            let response = { data = dataRows }
            let message = JsonConvert.SerializeObject(response)
            Console.WriteLine("Found {0} clusters\r\n{1}", Seq.length properties, message)
            return! OK message x
        }    

    /// Get -> URL properties with dimensions name=value
    let Get (engine : MyriadEngine) (x : HttpContext) =
        async {
            try
                Console.WriteLine("REQ: Get " + x.request.rawQuery)

                let kv = HttpUtility.ParseQueryString(x.request.rawQuery)
                let context = getContext (engine.GetDimension) kv

                let properties = engine.Get(kv.["property"], context)
                let response = { Requested = DateTimeOffset.UtcNow; Context = context; Properties = properties }            
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
        
        
    /// Set -> PUT w/ JSON data
    let Set (engine : MyriadEngine) (x : HttpContext) =
        async {            
            try
                Console.WriteLine("REQ: set " + x.request.rawQuery)

                

                return! OK "Set called" x
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