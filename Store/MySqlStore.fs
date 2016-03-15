namespace Myriad.Store

open System
open System.Collections.Generic
open System.Data
open System.Diagnostics
open System.Text

open MySql.Data.MySqlClient

open Myriad

module MySqlAccessor =
    let private ts = new TraceSource( "Myriad.Store", SourceLevels.Information )

    let private log (procedureName : String) ([<ParamArray>] parameters : IDbDataParameter array) =
        let builder = new StringBuilder("CALL " + procedureName + "(")
        let arguments = parameters |> Array.map (fun p -> String.Format("/*{0}*/ '{1}'", p.ParameterName, p.Value))
        builder.Append(String.Join(", ", arguments)) |> ignore
        builder.Append(");") |> ignore
        ts.TraceEvent( TraceEventType.Verbose, 0, builder.ToString() )

    let private setCommand(command : IDbCommand) (procedureName : String) ([<ParamArray>] parameters : IDbDataParameter array) =        
        log procedureName parameters
        command.CommandType <- CommandType.StoredProcedure
        command.CommandText <- procedureName
        parameters |> Array.iter (fun p -> command.Parameters.Add(p) |> ignore)
        command              

    let executeScalarCommand<'T>(connection : IDbConnection) (setCommand : IDbCommand -> IDbCommand)=
        use command = setCommand(connection.CreateCommand())
        let result = command.ExecuteScalar() 

        try
            result :?> 'T
        with
        | ex -> ts.TraceEvent(TraceEventType.Error, 0, ex.ToString())
                Unchecked.defaultof<'T>

    let executeScalar<'T>(connection : IDbConnection) (procedureName : String) ([<ParamArray>] parameters : IDbDataParameter array)=
        let commandFn = fun (command : IDbCommand) -> setCommand command procedureName parameters
        executeScalarCommand<'T> connection commandFn
        
    let addDimension (connectionString : String) (dimensionName : String) =
        let parameters : IDbDataParameter[] = [| new MySqlParameter("inName", dimensionName) |]
        use connection = new MySqlConnection(connectionString)
        connection.Open()
        executeScalar<int64> connection "sp_dimensions_merge" parameters
        
    let addMeasure (connectionString : String) (``measure`` : Measure) =
        let parameters : IDbDataParameter[] = [| new MySqlParameter("inDimensionId", ``measure``.Dimension.Id); new MySqlParameter("inValue", ``measure``.Value) |]
        use connection = new MySqlConnection(connectionString)
        connection.Open()        
        executeScalar<int64> connection "sp_measures_merge" parameters

        

type MySqlStore(connectionString : String) =
    static let ts = new TraceSource( "Myriad.Store", SourceLevels.Information )
    let cache = new MyriadCache()
    let store = new MyriadStore()    

    interface IMyriadStore with
        member x.Initialize() = x.Initialize()        
        member x.GetMetadata() = x.GetMetadata()
        member x.GetDimensions() = x.GetDimensions()    
        member x.GetDimension(dimensionName) = x.GetDimension(dimensionName)    
        member x.AddDimension(dimensionName) = x.AddDimension(dimensionName)
        member x.RemoveDimension(dimension) = x.RemoveDimension(dimension)
        member x.SetDimensionOrder(dimensions) = x.SetDimensionOrder(dimensions)
        member x.AddMeasure(``measure``) = x.AddMeasure(``measure``)
        member x.RemoveMeasure(``measure``) = x.RemoveMeasure(``measure``)
        member x.GetProperties(history) = x.GetProperties(history)
        member x.GetAny(propertyKey, context) = x.GetAny(propertyKey, context)
        member x.GetMatches(propertyKey, context) = x.GetMatches(propertyKey, context)
        member x.GetProperty(propertyKey, asOf) = x.GetProperty(propertyKey, asOf)
        member x.GetMeasureBuilder() = x.GetMeasureBuilder()
        member x.GetPropertyBuilder() = x.GetPropertyBuilder()
        member x.SetProperty(property) = x.SetProperty(property)
        member x.PutProperty(property) = x.PutProperty(property)

    member x.Initialize() =
        // TODO: Initialize internal cache from MySql
        ignore()
        
    member x.GetMetadata() = store.GetMetadata()       

    member x.GetDimensions() = store.GetDimensions()

    member x.GetDimension(dimensionName : String) = store.GetDimension dimensionName        

    member x.AddDimension(dimensionName : String) = 
        let newId(name : String) = MySqlAccessor.addDimension connectionString name
        store.AddDimension dimensionName newId
        
    member x.RemoveDimension(dimension : Dimension) = store.RemoveDimension dimension

    member x.AddMeasure(``measure`` : Measure) = 
        MySqlAccessor.addMeasure connectionString ``measure`` |> ignore
        store.AddMeasure ``measure``

    member x.RemoveMeasure(``measure`` : Measure) = store.RemoveMeasure ``measure`` 
    
    member x.SetDimensionOrder(orderedDimensions : Dimension list) =       
        // TODO: Implement dimension ordering
        orderedDimensions
//        let current = store.Dimensions |> Set.ofSeq
//        let proposed = orderedDimensions |> Set.ofList
//        // If this is not the same set, we cannot reorder
//        if current <> proposed then store.Dimensions |> List.ofSeq else setDimensionOrder orderedDimensions                       

    member x.GetProperties(history : MyriadHistory) = cache.GetProperties() |> Seq.toList        

    member x.GetAny(propertyKey : String, context : Context) = 
        match propertyKey with
        | key when String.IsNullOrEmpty(key) -> cache.GetAny(context)
        | key -> cache.GetAny(key, context)
    
    member x.GetMatches(propertyKey : String, context : Context) = 
        match propertyKey with
        | key when String.IsNullOrEmpty(key) -> cache.GetMatches(context)
        | key -> 
            let success, result = cache.TryFind(key, context)
            if result.IsNone then Seq.empty else [ result.Value ] |> Seq.ofList        
    
    member x.GetProperty(propertyKey : String, asOf : DateTimeOffset) = 
        cache.GetProperty(propertyKey, asOf)

    member x.GetMeasureBuilder() = 
        let dimensions = x.GetDimensions()
        let dimensionMap = dimensions |> Seq.map (fun d -> d.Name, d) |> Map.ofSeq
        MeasureBuilder(dimensionMap)

    member x.GetPropertyBuilder() = 
        PropertyBuilder(x.GetDimensions())

    member x.SetProperty(property : Property) =
        store.UpdateMeasures property
        cache.SetProperty property

    member x.PutProperty(value : PropertyOperation) =
        let pb = x.GetPropertyBuilder()
        let filter = [ store.PropertyDimension ]
        
        let add (key : string) = 
            let property = value.ToProperty(pb.OrderClusters, filter)
            new LockFreeList<Property>( [ property ] ) 

        let update (key : string) (current : LockFreeList<Property>) = 
            let currentProperty = current.Value.Head
            let filterMeasures(cluster) = PropertyOperation.FilterMeasures(cluster, filter)
            let applyOperations(current : Cluster list) (operation : Operation<Cluster>) =
                match operation with
                | Add(cluster) -> filterMeasures(cluster) :: current
                | Update(previous, updated) -> filterMeasures(updated) :: (current |> List.filter (fun c -> c <> previous))
                | Remove(cluster) -> current |> List.filter (fun c -> c <> cluster)
            let clusters = pb.OrderClusters (value.Operations |> List.fold applyOperations currentProperty.Clusters) 
            let property = Property.Create(currentProperty.Key, value.Description, value.Deprecated, value.Timestamp, clusters)
            current.Add property        

        let current = cache.AddOrUpdate(value.Key, add, update)
        let property = current.Value.Head
        store.UpdateMeasures property
        property
