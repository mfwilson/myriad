namespace Myriad.Core.Tests

open System
open Myriad
open Myriad.Store

open NUnit.Framework

[<TestFixture>]
type MyriadStoreTests() =

    let getStore() =
        let store = MemoryStore()
        let dimensions = [ "Customer"; "Environment"; "Application"; "Instance" ] |> List.map store.AddDimension 
        Assert.True(dimensions.Length = 4)
        Assert.True(store.GetDimensions().Length = 5)   // + Property dimension
        store, store.GetMeasureBuilder(), store.GetPropertyBuilder()       

    let testClusters(mb : MeasureBuilder) = 
        [
            (* 0 *) Cluster.Create("six", mb { yield "Instance", "sally" } )
            (* 1 *) Cluster.Create("five", mb { yield "Application", "notepad.exe"; yield "Instance", "wally" } )
            (* 2 *) Cluster.Create("four", Set.empty )
            (* 3 *) Cluster.Create("three", mb { yield "Customer", "ABC Corp"; yield "Instance", "wally" } )
            (* 4 *) Cluster.Create("two", mb { yield "Customer", "ABC Corp"; yield "Environment", "DEV"; yield "Instance", "rex" } )
            (* 5 *) Cluster.Create("one", mb { yield "Customer", "ABC Corp"; yield "Environment", "PROD"; yield "Instance", "rex" } )
        ]

    let assertExists collection item = Assert.True(collection |> Seq.exists (fun i -> i = item))
    let assertNotExists collection item = Assert.False(collection |> Seq.exists (fun i -> i = item))

    [<Test>]
    member x.SetProperty() =       
        let store, mb, pb = getStore()
        let property = pb.Create "test" Epoch.UtcNow [ testClusters(mb).[0] ]       
        let setProperty = store.SetProperty(property)        
        Assert.True(property.Equals(setProperty))

    [<Test>]
    member x.MergeRemove() =
        let store, mb, pb = getStore()
        let clusters = testClusters(mb)                        
        let property = pb.Create "my.key" Epoch.UtcNow clusters
        store.SetProperty(property) |> ignore
        
        let operation = { Key = "my.key"; Description = ""; Deprecated = false; Timestamp = Epoch.UtcNow; Operations = [ Remove(clusters.[3]); Remove(clusters.[4]) ] }

        let merged = store.PutProperty(operation)

        assertExists merged.Clusters clusters.[0]
        assertExists merged.Clusters clusters.[1]
        assertExists merged.Clusters clusters.[2]
        assertExists merged.Clusters clusters.[5]

        assertNotExists merged.Clusters clusters.[3]
        assertNotExists merged.Clusters clusters.[4]

    [<Test>]
    member x.MergeAdd() =
        let store, mb, pb = getStore()
        let clusters = testClusters(mb)
                
        let operation1 = { Key = "my.key"; Description = "desc"; Deprecated = false; Timestamp = Epoch.UtcNow; Operations = [ Add(clusters.[0]); Add(clusters.[1]) ] }

        let created = store.PutProperty(operation1)

        let operation2 = { Key = "my.key"; Description = "desc"; Deprecated = false; Timestamp = Epoch.UtcNow; Operations = [ Add(clusters.[0]); Add(clusters.[2]) ] }

        let merged = store.PutProperty(operation2)

        Assert.AreEqual(3, merged.Clusters.Length)
        assertExists merged.Clusters clusters.[0]
        assertExists merged.Clusters clusters.[1]
        assertExists merged.Clusters clusters.[2]

    [<Test>]
    member x.MergeUpdate() =
        let store, mb, pb = getStore()
        let clusters = testClusters(mb)
        let property = pb.Create "my.key" Epoch.UtcNow clusters
        store.SetProperty(property) |> ignore

        let update = Cluster.Create("forty-two", mb { yield "Customer", "XYZ Corp" } )

        let operation = { Key = "my.key"; Description = ""; Deprecated = false; Timestamp = Epoch.UtcNow; Operations = [ Update(clusters.[4], update) ] }

        let merged = store.PutProperty(operation)

        assertNotExists merged.Clusters clusters.[4]
        assertExists merged.Clusters update
