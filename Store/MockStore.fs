namespace Myriad.Store

open System 

open Newtonsoft.Json

open Myriad

type MockStore() =

    inherit MemoryStore()


    let properties = [| "my.office.key"; "my.property.key"; "nx.auditFile.filter" |]

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
            { Dimension = dimensions.[0]; Values = [| "PROD"; "UAT"; "DEV" |] };
            { Dimension = dimensions.[1]; Values = [| "Chicago"; "New York"; "London"; "Amsterdam"; "Paris"; "Berlin"; "Tokyo" |] };
            { Dimension = dimensions.[2]; Values = [| "Rook"; "Knight"; "Pawn"; "Bishop"; "King"; "Queen" |] };
            { Dimension = dimensions.[3]; Values = [| "mary"; "jimmy"; "rex"; "paulie"; "tommy" |] };
        ]

    let dimensionMap =
        internalList |> List.map (fun item -> item.Dimension.Name.ToLower(), item.Values ) |> Map.ofSeq

    let queryMap =
        dimensions |> Seq.map (fun d -> d.Name.ToLower(), d) |> Map.ofSeq

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

    let sampleProperties =
        let map = dimensions |> Seq.map (fun d -> d.Name, d) |> Map.ofSeq
        let mb = new MeasureBuilder(map)
        let setBuilder = new PropertyBuilder(dimensions)
        let utcNow = Epoch.UtcNow
        [ setBuilder.Create "my.property.key" utcNow (getFruitClusters mb);
          setBuilder.Create "my.office.key" utcNow (getOfficeClusters  mb); 
          setBuilder.Create "nx.auditFile.filter" utcNow (getNxClusters  mb) ]

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

    member x.Initialize() =
        base.Initialize()
        [ "Environment"; "Location"; "Application"; "Instance" ] |> List.iter (fun d -> x.AddDimension(d) |> ignore)

        //internalList |> List.iter ()


//    member x.GetProperties(history : MyriadHistory) = 
//        sampleProperties
//
//    member x.Dimensions with get() = dimensions
//
//    member x.DimensionMap with get() = dimensionMap
//
//    member x.SampleProperties with get() = sampleProperties
//
//    member x.GetDimension(key : String) = queryMap.TryFind (key.ToLower())
//
//    //member x.GetDimensions() =
//    //    JsonConvert.SerializeObject(internalList)
//
//    member x.GetDimensions() = dimensions 
//
//    member x.GetDimensionList() =
//        let dimensionList = dimensions |> List.map (fun d -> d.Name)
//        JsonConvert.SerializeObject(dimensionList)
//
//    member x.GetDimensionValues(dimension : String) =
//        let dimensionValues = x.DimensionMap.TryFind(dimension.ToLower())
//        if dimensionValues.IsNone then
//            JsonConvert.SerializeObject( [] : String list )
//        else
//            let dimensions = dimensionValues.Value |> Array.sort 
//            JsonConvert.SerializeObject( dimensions )
//
//    member x.GetProperties() =
//        JsonConvert.SerializeObject( properties )
//
//    member x.GetMetadata() =
//        let property = { Name = "Property"; Id = 0L }
//        List.append [ { Dimension = property; Values = properties } ] internalList
//        //JsonConvert.SerializeObject(metadata)
//
