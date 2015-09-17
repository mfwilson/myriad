namespace Myriad

open System

type Context = { AsOf : DateTimeOffset; Measures : Set<Measure> }

