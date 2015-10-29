namespace Myriad.Web

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client
open WebSharper.UI.Next.Server

[<JavaScript>]
module Client =
    open WebSharper.JavaScript
    open WebSharper.UI.Next.Client
    open WebSharper.Charting

    let RadarChart () =
        let labels =
            [| "Eating"; "Drinking"; "Sleeping";
               "Designing"; "Coding"; "Cycling"; "Running" |]
        let data1 = [|28.0; 48.0; 40.0; 19.0; 96.0; 27.0; 100.0|]
        let data2 = [|65.0; 59.0; 90.0; 81.0; 56.0; 55.0; 40.0|]

        let ch =
            Chart.Combine [
                Chart.Radar(Seq.zip labels data1)
                    .WithFillColor(Color.Rgba(151, 187, 205, 0.2))
                    .WithStrokeColor(Color.Rgba(151, 187, 205, 1.))
                    .WithPointColor(Color.Rgba(151, 187, 205, 1.))

                Chart.Radar(Seq.zip labels data2)
                    .WithFillColor(Color.Rgba(220, 220, 220, 0.2))
                    .WithStrokeColor(Color.Rgba(220, 220, 220, 1.))
                    .WithPointColor(Color.Rgba(220, 220, 220, 1.))
            ]
        Renderers.ChartJs.Render(ch, Size = Size(400, 400))

module MyriadSite =

    let MainPage =
        Application.SinglePage (
            fun ctx ->
                Content.Page(
                    Title = "Myriad Configuration",
                
                    Body = [
                        h2 [ text "Myriad Configuration Home Page" ]
                        aAttr [attr.href "/dimensions"] [text "Dimensions"]
                        div [client <@ Client.RadarChart() @>]
                    ])
        )


//        Application.SinglePage (fun ctx ->
//            Content.Page(
//                Body = [
//                    h2 [ text "Myriad Configuration Home Page" ]                
//                    aAttr [attr.href "/dimensions"] [text "Dimensions"]
//                ])
//        )
