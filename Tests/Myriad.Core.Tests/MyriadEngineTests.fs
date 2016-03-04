namespace Myriad.Core.Tests

open System
open Myriad
open Myriad.Store

open NUnit.Framework

[<TestFixture>]
type MyriadEngineTests() =

    
    [<Test>]
    member x.CreatesPropertySet() =
        let store = MockStore()
        let engine = MyriadEngine(store)

        let mb = engine.MeasureBuilder

        let clusters = [
            Cluster.Create("A", mb { yield "Environment", "London" } )
            Cluster.Create("B", mb { yield "Environment", "Berlin" } )
            Cluster.Create("C", mb { yield "Environment", "London" } )
        ]

        let property = engine.PropertyBuilder.Create "test" Epoch.UtcNow clusters

        assert (property.Clusters.Length = 2)

    [<Test>]
    member x.UpdateCache() =
        let store = MockStore()
        let engine = MyriadEngine(store)

        let mb = engine.MeasureBuilder

        let property = engine.Get("nx.auditFile.filter", DateTimeOffset.MaxValue).Value

        // Create new cluster value
        let newCluster = Cluster.Create("*.xls", mb { yield "Environment", "London" } )
        let newProperty = engine.PropertyBuilder.Create property.Key Epoch.UtcNow (newCluster :: property.Clusters)
        
        store.SetProperty(newProperty) |> ignore

        let property2 = engine.Get("nx.auditFile.filter", DateTimeOffset.MaxValue).Value

        assert (newProperty.Clusters.Length = property2.Clusters.Length)
        

