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
        let key = "my.property.key"
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

    [<Test>]
    member x.TryFind() =
        let dimensions = getDimensions()
        let dimensionMap = dimensions |> Seq.map (fun d -> d.Name, d) |> Map.ofSeq
        let mb = new MeasureBuilder(dimensionMap)

        let setBuilder = new ClusterSetBuilder(dimensions)

        let clusterSet = setBuilder.Create(getClusters(mb))

        let cache = new MyriadCache()
        cache.Insert(clusterSet)

        let context = { 
                AsOf = DateTimeOffset.UtcNow; 
                Measures = mb { yield "Environment", "PROD"; yield "Location", "New York"; yield "Application", "Bishop"; yield "Instance", "rex" }
            }

        let success, value = cache.TryFind("my.property.key", context)
        Assert.True(success)
        Assert.AreEqual("pear", value)
