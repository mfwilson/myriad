namespace Myriad.Store

open System 

open Newtonsoft.Json

open Myriad

type MockStore() =

    inherit MemoryStore()

    let getFruitClusters (mb : MeasureBuilder) =
        Seq.ofList
            [
                Cluster.Create("apple", mb { yield "Environment", "PROD"; yield "Location", "London"; yield "Instance", "rex" } )
                Cluster.Create("pear", mb { yield "Environment", "PROD"; yield "Instance", "rex" } )
                Cluster.Create("pecan", mb { yield "Instance", "rex" } )
                Cluster.Create("peach", mb { yield "Application", "Rook" } )
                Cluster.Create("strawberry", mb { yield "Location", "Chicago" } )
                Cluster.Create("apricot", mb { yield "Environment", "DEV" } )
                Cluster.Create("pumpkin", Set.empty )
            ]

    let getOfficeClusters (mb : MeasureBuilder) =
        Seq.ofList
            [
                Cluster.Create("pencil", mb { yield "Environment", "PROD"; yield "Location", "London"; yield "Instance", "rex" } )
                Cluster.Create("desk", mb { yield "Environment", "PROD"; yield "Instance", "rex" } )
                Cluster.Create("blotter", mb { yield "Environment", "UAT" } )
                Cluster.Create("coffee", mb { yield "Environment", "DEV" } )
                Cluster.Create("chair", mb { yield "Instance", "jimmy" } )
                Cluster.Create("ruler", mb { yield "Application", "Bishop" } )
                Cluster.Create("file", mb { yield "Location", "Chicago" } )
            ]

    let getNxClusters (mb : MeasureBuilder) =
        Seq.ofList
            [
                Cluster.Create("*.csv", mb { yield "Environment", "UAT"; yield "Location", "Tokyo";  } )
                Cluster.Create("*.txt", Set.empty )
            ]

    interface IMyriadStore with
        member x.Initialize() = x.Initialize()
        member x.GetMetadata() = base.GetMetadata()
        member x.GetDimensions() = base.GetDimensions()    
        member x.GetDimension(dimensionName) = base.GetDimension(dimensionName)    
        member x.AddDimension(dimensionName) = base.AddDimension(dimensionName)
        member x.RemoveDimension(dimension) = base.RemoveDimension(dimension)
        member x.AddMeasure(``measure``) = base.AddMeasure(``measure``)
        member x.RemoveMeasure(``measure``) = base.RemoveMeasure(``measure``)
        member x.GetProperties(history) = base.GetProperties(history)
        member x.GetAny(propertyKey, context) = base.GetAny(propertyKey, context)
        member x.GetMatches(propertyKey, context) = base.GetMatches(propertyKey, context)
        member x.GetProperty(propertyKey, asOf) = base.GetProperty(propertyKey, asOf)
        member x.GetMeasureBuilder() = base.GetMeasureBuilder()
        member x.GetPropertyBuilder() = base.GetPropertyBuilder()

    member x.Initialize() =
        base.Initialize()

        let createDimension (dimensionValues : string * string list) =
            let dimension = x.AddDimension(fst dimensionValues)
            (snd dimensionValues) |> List.iter (fun v -> x.AddMeasure { Dimension = dimension; Value = v } |> ignore)

        [ "Environment", [ "PROD"; "UAT"; "DEV" ];
          "Location",    [ "Chicago"; "New York"; "London"; "Amsterdam"; "Paris"; "Berlin"; "Tokyo" ];
          "Application", [ "Rook"; "Knight"; "Pawn"; "Bishop"; "King"; "Queen" ];
          "Instance",    [ "mary"; "jimmy"; "rex"; "paulie"; "tommy" ] ]
        |> List.iter (fun d -> createDimension d )

        let mb = x.GetMeasureBuilder()
        let pb = x.GetPropertyBuilder()

        let utcNow = Epoch.UtcNow
        [ pb.Create "my.property.key" utcNow (getFruitClusters mb);
          pb.Create "my.office.key" utcNow (getOfficeClusters  mb); 
          pb.Create "nx.auditFile.filter" utcNow (getNxClusters  mb) ]
        |> List.iter (fun p -> x.SetProperty p |> ignore)

