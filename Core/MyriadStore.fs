namespace Myriad

open System
open System.Collections.Concurrent
open System.Collections.Generic

type MyriadHistory =
    | All of unit
    | Depth of int
    | Time of TimeSpan

type IMyriadStore =
    abstract Initialize : unit -> unit

    abstract GetProperties : MyriadHistory -> Property list

    /// GetMetadata: return list of name+values where name is a dimension name and values contains the list of possible values
    ///   NOTE: this list should also contain "Property" and list of properties
    abstract GetMetadata : unit -> DimensionValues list 

    /// Get the list of dimensions (either dimension list or string list)
    abstract GetDimensions : unit -> IDimension list
    
    abstract GetDimension : string -> IDimension option

    // CreateDimension: create a new dimension and append to list of dimensions 
    // RemoveDimension: remove a dimension from the list of dimensions
    // OrderDimensions: re-order the list of dimensions

    // AddValue: given dimension or property name and value, add value to list of possible values
    // - If value already exists, do not add
    // RemoveValue: given dimension or property name and value, remove the value from the list of possible values
    // - If value used in current key set, do not remove (tricky)

    // Query: given a set of name-value pairs corresponding to dimensions, return matching clusters

type MemoryStore() =

    // Customer, Environment, Application, Instance
    let dimensions =
        [ { Dimension.Id = 1L; Name = "Customer" };
          { Dimension.Id = 2L; Name = "Environment" };          
          { Dimension.Id = 3L; Name = "Application" };
          { Dimension.Id = 4L; Name = "Instance" } ]

    let properties = new List<String>()

    let dimensionMap = new ConcurrentDictionary<IDimension, List<String>>()

    do
        dimensions |> Seq.iter (fun d -> dimensionMap.[d] <- new List<String>())

    interface IMyriadStore with
        member x.Initialize() = ignore()
        member x.GetProperties(history) = x.GetProperties(history)
        member x.GetMetadata() = x.GetMetadata()
        member x.GetDimensions() = x.GetDimensions()    
        member x.GetDimension(dimension) = x.GetDimension(dimension)

    member x.GetProperties(history : MyriadHistory) =
        []

    member x.GetMetadata() = 
        let property = { Name = "Property"; Id = 0L }
        let dimensionValues = dimensionMap |> Seq.map (fun kv -> { Dimension = kv.Key; Values = kv.Value |> Seq.toList } ) |> Seq.toList
        List.append [ { Dimension = property; Values = properties |> Seq.toList } ] dimensionValues

    member x.GetDimensions() = dimensions |> Seq.cast<IDimension> |> List.ofSeq

    member x.GetDimension(dimension) = None



    

    

    