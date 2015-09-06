namespace Myriad

open System
open System.Collections.Generic

// Dimension1, Dimension2 .. DimensionN + Key -> value
//
// Example dimensions
// Environment -> PROD, UAT, DEV
// Location -> Chicago, New York, London, Amsterdam
// Application -> Rook, Knight, Pawn, Bishop
// Instance -> mary, jimmy, rex
//
// Properties -> db_setting="ConnectionString", max_size="10", password="secret"
//

//type Dimension = { Id : Int32; Name : String }
//
//type DimensionSet = { Name : String; Dimensions : Dimension array }
//
//type Property = { Id : Int32; Name : String }
//
//type Cluster = { Id : Int32; Property : Property; Value : String; Dimensions : Dimension array }

//type ConfigurationValue = { Value : String }
//type ConfigurationKeySet = { Keys : ConfigurationKey array }

// 1. Take dimensions and determine weights based on ordering
//    - Dimensions are weighted in reverse order, e.g. Instance, Application, Location, Environment
//    - Weights are computed for each level as 2^n where n is the level,
//      e.g. Environment=1, Location=2, Application=4, Instance=8
//    - When no dimensions are specified, the key gets a weight of zero (effectively the value's default)
// 2. For a given property, apply the weighted ordering to its set of clusters, sorting from greatest to least
//    Dimensions: E L A I    Weight     Values
//                x x - x -> 11         apple
//                x - - x -> 9          pear
//                - - - x -> 8          pecan
//                - - x - -> 4          peach
//                - x - - -> 2          strawberry
//                x - - - -> 1          apricot
//                - - - - -> 0          pumpkin
// 3. Lookups are accomplished by, for a given key, matching the provided context against the set for the first
//    positive match; matches must match over all dimensions to be considered equal


