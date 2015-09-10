namespace Myriad

open System

type MeasureBuilder(map : Map<String, Dimension>) =
    member x.Bind(m, f) =  m |> List.collect f |> Set.ofList
    member x.Zero() = Set.empty        
    member x.Yield(m) = Set.ofList [ Measure(map.[fst(m)], snd(m)) ]
    member x.YieldFrom(m) = m
    member x.For(m,f) = x.Bind(m,f)        
    member x.Combine(a, b) = Set.union a b
    member x.Delay(f) = f()