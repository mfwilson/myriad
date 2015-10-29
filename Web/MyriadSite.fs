namespace Myriad.Web

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Server

module MyriadSite =

    let MainPage =
        Application.SinglePage (fun ctx ->
            Content.Page(
                Body = [
                    h2 [ text "Myriad Configuration Home Page" ]
                
                    aAttr [attr.href "/dimensions"] [text "Dimensions"]
                ])
        )
