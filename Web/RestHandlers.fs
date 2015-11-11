namespace Myriad.Web

open System
open System.Collections.Specialized
open System.Net
open System.Text
open System.Web

open Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.Successful
open Suave.Http.Writers
open Suave.Types

open Newtonsoft.Json

open Myriad

type AjaxResponse =
    { data : Map<String, String> seq }

module RestHandlers =

    let private getContext (getDimension : String -> IDimension option) (kv : NameValueCollection) =
        let getMeasure(key) = 
            let dimension = getDimension(key)
            if dimension.IsNone then None else Some( { Dimension = dimension.Value; Value = kv.[key] } )

        let measures = kv.AllKeys |> Seq.choose getMeasure |> Set.ofSeq
        { AsOf = DateTimeOffset.UtcNow; Measures = measures }

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
            Console.WriteLine("REQ: " + x.request.rawQuery)

            let kv = HttpUtility.ParseQueryString(x.request.rawQuery)

            let context = getContext (engine.GetDimension) kv

            let measuresAsString = context.Measures |> Seq.map (fun m -> m.ToString())
            Console.WriteLine("Measures: " + String.Join(", ", measuresAsString))

            let clusters = engine.Query(kv.["property"], context)
            let dimensions = engine.GetDimensions() 
            let dataRows = clusters |> Seq.mapi (fun i c -> Cluster.ToMap(c, dimensions, i))

            let response = { data = dataRows }
            let message = Newtonsoft.Json.JsonConvert.SerializeObject(response)
            Console.WriteLine("Found {0} clusters\r\n{1}", Seq.length clusters, message)
            return! OK message x
        }

    /// Find -> URL properties with dimensions name=value
    let Find (engine : MyriadEngine) (x : HttpContext) =
        async {
            let kv = HttpUtility.ParseQueryString(x.request.rawQuery)

            let context = getContext (engine.GetDimension) kv

            let clusters = engine.Find(kv.["property"], context)

            let builder = new StringBuilder("<body>")

            let measuresAsString = context.Measures |> Seq.map (fun m -> m.ToString())
            builder.AppendFormat("Context AsOf: {0}, Measures: {1}<p>", context.AsOf, String.Join(", ", measuresAsString)) |> ignore

            clusters |> Seq.iter (fun c -> builder.AppendFormat("Property: [{0}] = [{1}]<p>", c.Key, c.Value) |> ignore)

            builder.Append("</body>") |> ignore

            let message = builder.ToString()
            let query = x.request.url.Query
            return! OK message x
        }

