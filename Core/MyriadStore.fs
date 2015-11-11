namespace Myriad

open System

type MyriadHistory =
    | All of unit
    | Depth of int
    | Time of TimeSpan

type IMyriadStore =
    abstract Initialize : unit -> unit

    abstract GetClusterSets : MyriadHistory -> ClusterSet list

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




    // Cache

    // Query: given a set of name-value pairs corresponding to dimensions, return matching clusters



    

    

    