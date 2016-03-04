namespace Myriad.Core.Tests

open System
open Myriad
open Myriad.Store

open NUnit.Framework

[<TestFixture>]
type MyriadStoreTests() =

    [<Test>]
    member x.SetProperty() =       
        let store = MemoryStore()

        let dimensions = [ "Customer"; "Environment"; "Application"; "Instance" ] |> List.map store.AddDimension 
        assert (dimensions.Length = 4)
        assert (store.GetDimensions().Length = 5)   // + Property dimension

        let mb = store.GetMeasureBuilder()
        let pb = store.GetPropertyBuilder()
        
        let cluster = Cluster.Create("12345", mb { yield "Customer", "ABC Corp"; yield "Environment", "PROD"; yield "Instance", "rex" } )
        
        let property = pb.Create "test" Epoch.UtcNow [ cluster ]       
        let setProperty = store.SetProperty(property)        
        assert (property = setProperty)

    [<Test>]
    member x.MergeAdd() =
        let store = MemoryStore()

        let dimensions = [ "Customer"; "Environment"; "Application"; "Instance" ] |> List.map store.AddDimension 

        let mb = store.GetMeasureBuilder()
        let pb = store.GetPropertyBuilder()
                
        let cluster1 = Cluster.Create("one", mb { yield "Customer", "ABC Corp"; yield "Environment", "PROD"; yield "Instance", "rex" } )
        let cluster2 = Cluster.Create("two", mb { yield "Customer", "ABC Corp"; yield "Environment", "DEV"; yield "Instance", "rex" } )
        let cluster3 = Cluster.Create("three", mb { yield "Customer", "ABC Corp"; yield "Instance", "wally" } )

        let operation1 = { Key = "my.key"; Description = "desc"; Deprecated = false; Timestamp = Epoch.UtcNow; Operations = [ Add(cluster1); Add(cluster2) ] }

        let created = store.MergeProperty(operation1)

        let operation2 = { Key = "my.key"; Description = "desc"; Deprecated = false; Timestamp = Epoch.UtcNow; Operations = [ Add(cluster1); Add(cluster3) ] }

        let merged = store.MergeProperty(operation2)


        
        
        ignore()
