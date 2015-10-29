namespace Myriad.Web

open System
open System.Net
open System.Text
open System.Web

open Suave
open Suave.Http
open Suave.Http.Successful 
open Suave.Types

open Myriad
open Myriad.Store

module RestHandlers =

    /// Provides a view over dimensions and their values
    let Dimensions (store : MockStore) (x : HttpContext) = 
        async {
            let getHtmlBody(absolutePath : String) =
                let dimension = 
                    match absolutePath.LastIndexOf("/") with
                    | -1 -> ""
                    | index -> absolutePath.Substring(index + 1)

                let dimensionValues = store.DimensionMap.TryFind(dimension.ToLower())
                if dimensionValues.IsNone then
                    "<body>No values for for dimension '" + dimension + "'</body>"
                else
                    let dimensions = dimensionValues.Value |> List.sort 
                    "<body>" + String.Join("<p>", dimensions) + "</body>"

            let message = match x.request.url.AbsolutePath with
                            | "/dimensions" ->
                                let names = store.Dimensions |> Seq.map (fun d -> String.Format("<a href='/dimensions/{0}'>{0}</a>", d.Name)) 
                                "<body>" + String.Join("<p>", names) + "</body>"
                            | dimension -> getHtmlBody(dimension)
                                
            return! OK message x
        }

    /// Query -> JSON w/ context
    let Query (cache : MyriadCache) (store : MockStore) (x : HttpContext) =
        async {
            return! OK "Not implemented" x
        }

    /// Find -> URL properties with dimensions name=value
    let Find (cache : MyriadCache) (store : MockStore) (x : HttpContext) =
        async {
            let kv = HttpUtility.ParseQueryString(x.request.rawQuery)
            
            let getMeasure(key) = 
                let dimension = store.GetDimension(key)
                if dimension.IsNone then None else Some( { Dimension = dimension.Value; Value = kv.[key] } )

            let measures = kv.AllKeys 
                            |> Seq.choose getMeasure
                            |> Set.ofSeq
                         
            let context = { AsOf = DateTimeOffset.UtcNow; Measures = measures }

            let clusters = match kv.["property"] with
                            | propertyKey when String.IsNullOrEmpty(propertyKey) -> cache.GetProperties(context)
                            | propertyKey -> 
                                let success, result = cache.TryFind(propertyKey, context)
                                if result.IsNone then Seq.empty else [ result.Value ] |> Seq.ofList

            let builder = new StringBuilder("<body>")

            let measuresAsString = measures |> Seq.map (fun m -> m.ToString())
            builder.AppendFormat("Context AsOf: {0}, Measures: {1}<p>", context.AsOf, String.Join(", ", measuresAsString)) |> ignore

            clusters |> Seq.iter (fun c -> builder.AppendFormat("Property: [{0}] = [{1}]<p>", c.Key, c.Value) |> ignore)

            builder.Append("</body>") |> ignore

            let message = builder.ToString()
            let query = x.request.url.Query
            return! OK message x
        }

