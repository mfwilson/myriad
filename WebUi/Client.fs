namespace WebUi

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI.Next
open WebSharper.UI.Next.Client
open WebSharper.UI.Next.Formlets
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Notation
//open WebSharper.UI.Next.SiteCommon


[<AutoOpen>]
[<JavaScript>]
module internal Utilities =

    /// Class attribute
    let cls n = Attr.Class n

    /// Style attribute
    let sty n v = Attr.Style n v

    /// Div with single class
    let divc c docs = Doc.Element "div" [cls c] docs

    /// Link with click callback
    let href txt url = Doc.Element "a" [attr.href url] [text txt]

[<AutoOpen>]
[<JavaScript>]
module SiteCommon =
    type PageTy = | Home | About | Samples

    type Meta =
        {
            FileName : string
            Keywords : list<string>
            Title : string
            Uri : string
        }

    type Sample =
        {
            mutable Body : Doc
            mutable Description : Doc
            Meta : Meta
            mutable Router : Router<Page>
            mutable RouteId : RouteId
            mutable SamplePage : Page
        }
    and Page =
        {
            mutable PageName : string
            mutable PageRouteId : RouteId
            //mutable PageRender : Doc
            mutable PageType : PageTy
            mutable PageSample : Sample option
            //mutable PageRenderVar : Var<PageTy>
        }

    let mkPage name routeId ty = //render ty rv =
        {
            PageName = name
            PageRouteId = routeId
           // PageRender = render
            PageType = ty
            PageSample = None
            //PageRenderVar = rv
        }

[<JavaScript>]
module Samples =

    // First, define the samples type, which specifies metadata and a rendering
    // function for each of the samples.
    // A Sample consists of a file name, identifier, list of keywords,
    // rendering function, and title.

    type Visuals<'T> =
        {
            Desc : 'T -> Doc
            Main : 'T -> Doc
        }

    let Sidebar vPage samples =
        let renderItem sample =
            let attrView =
                View.FromVar vPage
                |> View.Map (fun pg -> pg.PageSample)
            let pred s = Option.exists (fun smp -> sample.Meta.FileName = smp.Meta.FileName) s
            let activeAttr = Attr.DynamicClass "active" attrView pred
            Doc.Link sample.Meta.Title
                [cls "list-group-item"; activeAttr]
                (fun () -> Var.Set vPage sample.SamplePage)
                :> Doc

        divc "col-md-3" [
            h4 [text "Samples"]
            List.map renderItem samples |> Doc.Concat
        ]

    let RenderContent sample =
        divc "samples col-md-9" [
            div [
                divc "row" [
                    h1 [text sample.Meta.Title]
                    div [
                        p [ sample.Description ]
                        p [
                            aAttr
                                [ attr.href ("https://github.com/intellifactory/websharper.ui.next.samples/blob/master/src/" + sample.Meta.Uri + ".fs") ]
                                [text "View Source"]
                        ]
                    ]
                ]

                divc "row" [
                    p [ sample.Body ]
                ]
            ]
        ]

    let Render vPage pg samples =
        let sample =
            match pg.PageSample with
            | Some s -> s
            | None -> failwith "Attempted to render non-sample on samples page"

        sectionAttr [cls "block-small"] [
            divc "container" [
                divc "row" [
                    Sidebar vPage samples
                    RenderContent sample
                ]
            ]
        ]

    let CreateRouted router init vis meta =
        let sample =
            {
                Body = Doc.Empty
                Description = Doc.Empty
                Meta = meta
                Router = Unchecked.defaultof<_>
                RouteId = Unchecked.defaultof<_>
                SamplePage = Unchecked.defaultof<_>
            }
        let r =
             Router.Route router init (fun id cur ->
                sample.RouteId <- id
                sample.Body <- vis.Main cur
                sample.Description <- vis.Desc cur
                let page = mkPage sample.Meta.Title id Samples
                page.PageSample <- Some sample
                page.PageRouteId <- id
                sample.SamplePage <- page
                page
             )
             |> Router.Prefix meta.Uri
        sample.Router <- r
        sample

    let CreateSimple vis meta =
        let unitRouter = RouteMap.Create (fun () -> []) (fun _ -> ())
        let sample =
            {
                Body = vis.Main ()
                Description = vis.Desc ()
                Meta = meta
                Router = Unchecked.defaultof<_>
                RouteId = Unchecked.defaultof<_>
                SamplePage = Unchecked.defaultof<_>
            }

        sample.Router <-
            // mkPage name routeId ty
            Router.Route unitRouter () (fun id cur ->
                let page = mkPage sample.Meta.Title id Samples
                sample.RouteId <- id
                page.PageSample <- Some sample
                page.PageRouteId <- id
                sample.SamplePage <- page
                page)
            |> Router.Prefix meta.Uri
        sample

    [<Sealed>]
    type Builder<'T>(create: Visuals<'T> -> Meta -> Sample) =

        let mutable meta =
            {
                FileName = "Unknown.fs"
                Keywords = []
                Title = "Unknown"
                Uri = "unknown"
            }

        let mutable vis =
            {
                Desc = fun _ -> Doc.Empty
                Main = fun _ -> Doc.Empty
            }

        member b.Create () =
            create vis meta

        member b.FileName x =
            meta <- { meta with FileName = x }; b

        member b.Id x =
            meta <- { meta with Title = x; Uri = x }; b

        member b.Keywords x =
            meta <- { meta with Keywords = x }; b

        member b.Render f =
            vis <- { vis with Main = (fun x -> f x :> Doc) }; b

        member b.RenderDescription f =
            vis <- { vis with Desc = (fun x -> f x :> Doc) }; b

        member b.Title x =
            meta <- { meta with Title = x }; b

        member b.Uri x =
            meta <- { meta with Uri = x }; b

    let Build () =
        Builder CreateSimple

    let Routed (router, init) =
        Builder (CreateRouted router init)

    let InitialSamplePage samples =
        (List.head samples).SamplePage

    let SamplesRouter samples =
        Router.Merge [ for s in samples -> s.Router ]
        |> Router.Prefix "samples"

[<JavaScript>]
module Client =
    
    // First, we declare types for phones and how to order them.

    type Phone = { Name: string; Snippet: string; Age: int }
    type Order = Alphabetical | Newest

    type Order with

        /// A textual representation of our orderings.
        static member Show order =
            match order with
            | Alphabetical -> "Alphabetical"
            | Newest -> "Newest"

    type Phone with

        /// A comparison function, based on whether we're sorting by name or age.
        static member Compare order p1 p2 =
            match order with
            | Alphabetical -> compare p1.Name p2.Name
            | Newest -> compare p1.Age p2.Age

        /// A filtering function.
        static member MatchesQuery q ph =
            ph.Name.Contains(q)
            || ph.Snippet.Contains(q)

//    let DimensionWidget(name : String) (values : String list) =
//        // Firstly, we make a reactive variable for the list of phones.
//        let allPhones = Var.Create values
//        // and one for the query string
//        let query = Var.Create ""
//        // And one for the ordering.
//        let order = Var.Create Newest
//
//        // The above vars are our model. Everything else is computed from them.
//        // Now, compute visible phones under the current selection:
//        let visiblePhones =
//            View.Map2 (fun query order ->
//                values
//                |> List.sort
//                (View.FromVar query)
//                (View.FromVar order)
//
//        // A simple function for displaying the details of a phone:
////        let showPhone ph =
////            li [
////                span [ text ph.Name ]
////                p [ text ph.Snippet ]
////            ] :> Doc
//
//        //let showPhones phones =
//        //    Doc.Concat (List.map showPhone phones)
//
//        divc "row" [
//
//            divc "col-sm-6" [
//
//                // We specify a label, and an input box linked to our query RVar.
//                text "Search: "
//                Doc.Input [Attr.Create "class" "form-control"] query
//
//                // We then have a select box, linked to our orders variable
//                
//                Doc.Select [Attr.Create "class" "form-control"] Order.Show [Newest; Alphabetical] order
//
//                divc "col-sm-6" [
//                    ul [ Doc.EmbedView (View.Map showPhones visiblePhones) ]
//                ]
//            ]
//        ]
        

    // This is our phones widget. We take a list of phones, and return
    // an document tree which can be rendered.
    let PhonesWidget (phones: list<Phone>) =
        // Firstly, we make a reactive variable for the list of phones.
        let allPhones = Var.Create phones
        // and one for the query string
        let query = Var.Create ""
        // And one for the ordering.
        let order = Var.Create Newest

        // The above vars are our model. Everything else is computed from them.
        // Now, compute visible phones under the current selection:
        let visiblePhones =
            View.Map2 (fun query order ->
                phones
                |> List.filter (Phone.MatchesQuery query)
                |> List.sortWith (Phone.Compare order))
                (View.FromVar query)
                (View.FromVar order)

        // A simple function for displaying the details of a phone:
        let showPhone ph =
            li [
                span [ text ph.Name ]
                p [ text ph.Snippet ]
            ] :> Doc

        let showPhones phones =
            Doc.Concat (List.map showPhone phones)

        // The main body.
        divc "row" [

            divc "col-sm-6" [

                // We specify a label, and an input box linked to our query RVar.
                text "Search: "
                Doc.Input [Attr.Create "class" "form-control"] query

                // We then have a select box, linked to our orders variable
                text "Sort by: "
                Doc.Select [Attr.Create "class" "form-control"] Order.Show [Newest; Alphabetical] order

                // Finally, we render the list of phones using RD.ForEach.
                // When the list changes, the DOM will be updated to reflect this.
                divc "col-sm-6" [
                    ul [ Doc.EmbedView (View.Map showPhones visiblePhones) ]
                ]
            ]

        ]

    let Main () =

        let dimensions = Server.GetDimensions()

        // Here, we make a couple of phones, and declare a phonesWidget, then run the example.
        let defPhone name snip age =
            {
                Age = age
                Name = name
                Snippet = snip
            }
        PhonesWidget [
            defPhone "Nexus S" "Fast just got faster with Nexus S." 1
            defPhone "Motorola XOOM" "The Next, Next generation tablet" 2
            defPhone "Motorola XOOM with Wi-Fi" "The Next, Next generation tablet" 3
            defPhone "Samsung Galaxy" "The Ultimate Phone" 4
        ]

    // Todo: I don't like this. There's got to be a nicer way of embedding links.
    let Description () =
        div [
            text "Taken from the "
            href "AngularJS Tutorial" "https://docs.angularjs.org/tutorial/"
            text ", a list filtering and sorting application for phones."
        ]

    // Boilerplate..
    let Sample =
        Samples.Build()
            .Id("PhoneExample")
            .FileName(__SOURCE_FILE__)
            .Keywords(["todo"])
            .Render(Main)
            .RenderDescription(Description)
            .Create()


//    let Main () =
//        let rvInput = Var.Create ""
//        let submit = Submitter.CreateOption rvInput.View
//        let vReversed =
//            submit.View.MapAsync(function
//                | None -> async { return "" }
//                | Some input -> Server.DoSomething input
//            )
//        div [
//            Doc.Input [] rvInput
//            Doc.Button "Send" [] submit.Trigger
//            hr []
//            h4Attr [attr.``class`` "text-muted"] [text "The server responded:"]
//            divAttr [attr.``class`` "jumbotron"] [h1 [textView vReversed]]
//        ]
