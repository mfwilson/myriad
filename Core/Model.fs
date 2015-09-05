namespace Myriad

open System
open System.Collections.Concurrent

// string (key) -> Int64 (time) list -> sorted Cluster list 

//

type DimensionCache() = 
    
    let cache = new ConcurrentDictionary<String, Int64>()
    
    
    member this.X = "F#"
