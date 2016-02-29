namespace Myriad.Core.Tests

open System
open Myriad

open NUnit.Framework

[<TestFixture>]
type MyriadCacheTests() =

    // Environment -> PROD, UAT, DEV
    // Location -> Chicago, New York, London, Amsterdam
    // Application -> Rook, Knight, Pawn, Bishop
    // Instance -> mary, jimmy, rex
    let getDimensions() =
        [ { Dimension.Id = 32L; Name = "Environment" };
          { Dimension.Id = 21L; Name = "Location" };
          { Dimension.Id = 44L; Name = "Application" };
          { Dimension.Id = 98L; Name = "Instance" } ] 
        |> Seq.cast<IDimension>

    let getClusters(mb : MeasureBuilder) =
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

    [<Test>]
    member x.TryGetValue() =
        let dimensions = getDimensions()
        let dimensionMap = dimensions |> Seq.map (fun d -> d.Name, d) |> Map.ofSeq
        let mb = new MeasureBuilder(dimensionMap)

        let setBuilder = new PropertyBuilder(dimensions)

        let clusterSet = setBuilder.Create("my.property.key", getClusters(mb))

        let cache = new MyriadCache()
        cache.Insert(clusterSet)

        let context = { 
                AsOf = DateTimeOffset.UtcNow; 
                Measures = mb { yield "Environment", "PROD"; yield "Location", "New York"; yield "Application", "Bishop"; yield "Instance", "rex" }
            }

        let success, value = cache.TryGetValue("my.property.key", context)
        Assert.True(success)
        Assert.AreEqual("pear", value)
