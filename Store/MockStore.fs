﻿namespace Myriad.Store

open System 

open Myriad

type MockStore() =

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
    let dimensionMap = 
        let elements = 
            [
                dimensions.[0].Name.ToLower(), [ "PROD"; "UAT"; "DEV" ];
                dimensions.[1].Name.ToLower(), [ "Chicago"; "New York"; "London"; "Amsterdam" ];
                dimensions.[2].Name.ToLower(), [ "Rook"; "Knight"; "Pawn"; "Bishop" ];
                dimensions.[3].Name.ToLower(), [ "mary"; "jimmy"; "rex"; "paulie" ];
            ]
        Map<String, String list>(elements)

    let queryMap =
        dimensions |> Seq.map (fun d -> d.Name.ToLower(), d :> IDimension) |> Map.ofSeq

    let getFruitClusters (key : String) (mb : MeasureBuilder) =
        Seq.ofList
            [
                Cluster(0L, key, "apple", mb { yield "Environment", "PROD"; yield "Location", "London"; yield "Instance", "rex" } )
                Cluster(0L, key, "pear", mb { yield "Environment", "PROD"; yield "Instance", "rex" } )
                Cluster(0L, key, "pecan", mb { yield "Instance", "rex" } )
                Cluster(0L, key, "peach", mb { yield "Application", "Rook" } )
                Cluster(0L, key, "strawberry", mb { yield "Location", "Chicago" } )
                Cluster(0L, key, "apricot", mb { yield "Environment", "DEV" } )
                Cluster(0L, key, "pumpkin", Set.empty )
            ]

    let getOfficeClusters (key : String) (mb : MeasureBuilder) =
        Seq.ofList
            [
                Cluster(0L, key, "pencil", mb { yield "Environment", "PROD"; yield "Location", "London"; yield "Instance", "rex" } )
                Cluster(0L, key, "desk", mb { yield "Environment", "PROD"; yield "Instance", "rex" } )
                Cluster(0L, key, "blotter", mb { yield "Environment", "UAT" } )
                Cluster(0L, key, "coffee", mb { yield "Environment", "DEV" } )
                Cluster(0L, key, "chair", mb { yield "Instance", "jimmy" } )
                Cluster(0L, key, "ruler", mb { yield "Application", "Bishop" } )
                Cluster(0L, key, "file", mb { yield "Location", "Chicago" } )
            ]

    let sampleProperties =
        let map = dimensions |> Seq.map (fun d -> d.Name, d :> IDimension) |> Map.ofSeq
        let mb = new MeasureBuilder(map)
        let setBuilder = new ClusterSetBuilder(dimensions |> Seq.cast<IDimension>)

        [ setBuilder.Create(getFruitClusters "my.property.key" mb)
          setBuilder.Create(getOfficeClusters "my.office.key" mb) ]

    member x.Dimensions with get() = dimensions

    member x.DimensionMap with get() = dimensionMap

    member x.SampleProperties with get() = sampleProperties

    member x.GetDimension(key) = queryMap.TryFind key