namespace Myriad.Store

open System 

open Newtonsoft.Json

open Myriad

type MockStore() =

    let properties = [ "my.office.key"; "my.property.key"; "nx.auditFile.filter" ]

    // Environment, Location, Application, Instance
    let dimensions =
        [ { Dimension.Id = 32L; Name = "Environment" };
          { Dimension.Id = 21L; Name = "Location" };
          { Dimension.Id = 44L; Name = "Application" };
          { Dimension.Id = 98L; Name = "Instance" } ]

    // Environment -> PROD, UAT, DEV
    // Location -> Chicago, New York, London, Amsterdam
    // Application -> Rook, Knight, Pawn, Bishop
    // Instance -> mary, jimmy, rex, paulie
    let internalList = 
        [
            { Dimension = dimensions.[0]; Values = [ "PROD"; "UAT"; "DEV" ] };
            { Dimension = dimensions.[1]; Values = [ "Chicago"; "New York"; "London"; "Amsterdam"; "Paris"; "Berlin"; "Tokyo" ] };
            { Dimension = dimensions.[2]; Values = [ "Rook"; "Knight"; "Pawn"; "Bishop"; "King"; "Queen" ] };
            { Dimension = dimensions.[3]; Values = [ "mary"; "jimmy"; "rex"; "paulie"; "tommy" ] };
        ]

    let dimensionMap =
        internalList |> List.map (fun item -> item.Dimension.Name.ToLower(), item.Values ) |> Map.ofSeq

    let queryMap =
        dimensions |> Seq.map (fun d -> d.Name.ToLower(), d :> IDimension) |> Map.ofSeq

    let getFruitClusters (mb : MeasureBuilder) =
        Seq.ofList
            [
                Cluster(0L, "apple", mb { yield "Environment", "PROD"; yield "Location", "London"; yield "Instance", "rex" } )
                Cluster(0L, "pear", mb { yield "Environment", "PROD"; yield "Instance", "rex" } )
                Cluster(0L, "pecan", mb { yield "Instance", "rex" } )
                Cluster(0L, "peach", mb { yield "Application", "Rook" } )
                Cluster(0L, "strawberry", mb { yield "Location", "Chicago" } )
                Cluster(0L, "apricot", mb { yield "Environment", "DEV" } )
                Cluster(0L, "pumpkin", Set.empty )
            ]

    let getOfficeClusters (mb : MeasureBuilder) =
        Seq.ofList
            [
                Cluster(0L, "pencil", mb { yield "Environment", "PROD"; yield "Location", "London"; yield "Instance", "rex" } )
                Cluster(0L, "desk", mb { yield "Environment", "PROD"; yield "Instance", "rex" } )
                Cluster(0L, "blotter", mb { yield "Environment", "UAT" } )
                Cluster(0L, "coffee", mb { yield "Environment", "DEV" } )
                Cluster(0L, "chair", mb { yield "Instance", "jimmy" } )
                Cluster(0L, "ruler", mb { yield "Application", "Bishop" } )
                Cluster(0L, "file", mb { yield "Location", "Chicago" } )
            ]

    let getNxClusters (mb : MeasureBuilder) =
        Seq.ofList
            [
                Cluster(0L, "*.csv", mb { yield "Environment", "UAT"; yield "Location", "Tokyo";  } )
                Cluster(0L, "*.txt", Set.empty )
            ]

    let sampleProperties =
        let map = dimensions |> Seq.map (fun d -> d.Name, d :> IDimension) |> Map.ofSeq
        let mb = new MeasureBuilder(map)
        let setBuilder = new PropertyBuilder(dimensions |> Seq.cast<IDimension>)

        [ setBuilder.Create("my.property.key", getFruitClusters mb)
          setBuilder.Create("my.office.key", getOfficeClusters  mb) 
          setBuilder.Create("nx.auditFile.filter", getNxClusters  mb) ]

    interface IMyriadStore with
        member x.Initialize() = ignore()
        member x.GetProperties(history) = x.GetProperties(history)
        member x.GetDimensions() = x.GetDimensions()
        member x.GetDimension(key) = x.GetDimension(key)
        member x.GetMetadata() = x.GetMetadata()

    member x.GetProperties(history : MyriadHistory) = 
        sampleProperties

    member x.Dimensions with get() = dimensions

    member x.DimensionMap with get() = dimensionMap

    member x.SampleProperties with get() = sampleProperties

    member x.GetDimension(key : String) = queryMap.TryFind (key.ToLower())

    //member x.GetDimensions() =
    //    JsonConvert.SerializeObject(internalList)

    member x.GetDimensions() = dimensions |> Seq.cast<IDimension> |> Seq.toList

    member x.GetDimensionList() =
        let dimensionList = dimensions |> List.map (fun d -> d.Name)
        JsonConvert.SerializeObject(dimensionList)

    member x.GetDimensionValues(dimension : String) =
        let dimensionValues = x.DimensionMap.TryFind(dimension.ToLower())
        if dimensionValues.IsNone then
            JsonConvert.SerializeObject( [] : String list )
        else
            let dimensions = dimensionValues.Value |> List.sort 
            JsonConvert.SerializeObject( dimensions )

    member x.GetProperties() =
        JsonConvert.SerializeObject( properties )

    member x.GetMetadata() =
        let property = { Name = "Property"; Id = 0L }
        List.append [ { Dimension = property; Values = properties } ] internalList
        //JsonConvert.SerializeObject(metadata)

