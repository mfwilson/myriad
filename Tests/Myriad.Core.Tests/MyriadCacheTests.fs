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
        [ { Dimension.Id = 32UL; Name = "Environment" };
          { Dimension.Id = 21UL; Name = "Location" };
          { Dimension.Id = 44UL; Name = "Application" };
          { Dimension.Id = 98UL; Name = "Instance" } ]         

    let getClusters(mb : MeasureBuilder) =
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

    [<Test>]
    member x.TryGetValue() =
        let dimensions = getDimensions()
        let dimensionMap = dimensions |> Seq.map (fun d -> d.Name, d) |> Map.ofSeq
        let mb = new MeasureBuilder(dimensionMap)

        let setBuilder = new PropertyBuilder(dimensions)

        let clusterSet = setBuilder.Create "my.property.key" Epoch.UtcNow (getClusters mb)

        let cache = new MyriadCache()
        cache.SetProperty(clusterSet) |> ignore

        let context = { 
                AsOf = DateTimeOffset.UtcNow; 
                Measures = mb { yield "Environment", "PROD"; yield "Location", "New York"; yield "Application", "Bishop"; yield "Instance", "rex" }
            }

        let success, value = cache.TryGetValue("my.property.key", context)
        Assert.True(success)
        Assert.AreEqual("pear", value)
