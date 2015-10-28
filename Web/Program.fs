open System
open System.Net
open System.Text
open System.Web

open Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.Successful 
open Suave.Types
open Suave.Web

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Server
open WebSharper.Suave

open Myriad
open Myriad.Store

let store = new MockStore()

let cache = new MyriadCache()
store.SampleProperties |> Seq.iter cache.Insert

let dimensionsApp = 
    fun (x : HttpContext) ->
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

/// find -> JSON w/ context
let findApp =
    fun (x : HttpContext) ->
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

//[<JavaScript>]
//module Client =
//    open WebSharper.JavaScript
//    open WebSharper.UI.Next.Client
//    open WebSharper.Charting
//
//    let RadarChart () =
//        let labels =    
//            [| "Eating"; "Drinking"; "Sleeping";
//               "Designing"; "Coding"; "Cycling"; "Running" |]
//        let data1 = [|28.0; 48.0; 40.0; 19.0; 96.0; 27.0; 100.0|]
//        let data2 = [|65.0; 59.0; 90.0; 81.0; 56.0; 55.0; 40.0|]
//
//        let ch =
//            Chart.Combine [
//                Chart.Radar(Seq.zip labels data1)
//                    .WithFillColor(Color.Rgba(151, 187, 205, 0.2))
//                    .WithStrokeColor(Color.Rgba(151, 187, 205, 1.))
//                    .WithPointColor(Color.Rgba(151, 187, 205, 1.))
//
//                Chart.Radar(Seq.zip labels data2)
//                    .WithFillColor(Color.Rgba(220, 220, 220, 0.2))
//                    .WithStrokeColor(Color.Rgba(220, 220, 220, 1.))
//                    .WithPointColor(Color.Rgba(220, 220, 220, 1.))
//            ]
//        Renderers.ChartJs.Render(ch, Size = Size(400, 400))

open WebSharper.JavaScript

let MySite =    
    Application.SinglePage (fun ctx ->
        Content.Page(
            Body = [
                h2 [ text "Myriad Configuration Home Page" ]
                
                aAttr [attr.href "/dimensions"] [text "Dimensions"]
                
                //div [client <@ Client.RadarChart() @>]
            ])
    )

let app =
    choose [        
        //GET >>= path "/" >>= OK "<body><H2>Myriad Configuration Home Page</H2><a href='/dimensions'>Dimensions</a></body>"
        GET >>= path "/" >>= (WebSharperAdapter.ToWebPart MySite)
        path "/find" >>= findApp
        pathStarts "/dimensions" >>= dimensionsApp
        path "/ws" >>= (WebSharperAdapter.ToWebPart MySite)
    ]

let port = Sockets.Port.Parse("8083")

let serverConfig = 
    { defaultConfig with
       bindings = [ HttpBinding.mk HTTP IPAddress.Any port ]
    }

[<EntryPoint>]
let main argv =
    startWebServer serverConfig app //(WebSharperAdapter.ToWebPart MySite)
    0 
