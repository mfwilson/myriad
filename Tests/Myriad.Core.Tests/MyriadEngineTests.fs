namespace Myriad.Core.Tests

open System
open Myriad

open NUnit.Framework

[<TestFixture>]
type MyriadEngineTests() =


    [<Test>]
    member x.History() =


        let engine = MyriadEngine(MemoryStore())

        engine.GetDimensions() |> ignore
        

