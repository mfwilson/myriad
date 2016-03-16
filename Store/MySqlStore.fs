namespace Myriad.Store

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Data
open System.Diagnostics
open System.Runtime.InteropServices
open System.Text

open MySql.Data.MySqlClient
open Newtonsoft.Json

open Myriad

module MySqlAccessor =
    let private ts = new TraceSource( "Myriad.Store", SourceLevels.Information )

    let private updateUser = Environment.UserName

    let private log (procedureName : String) ([<ParamArray>] parameters : IDbDataParameter array) =
        let builder = new StringBuilder("CALL " + procedureName + "(")
        let arguments = parameters |> Array.map (fun p -> String.Format("/*{0}*/ '{1}'", p.ParameterName, p.Value))
        builder.Append(String.Join(", ", arguments)) |> ignore
        builder.Append(");") |> ignore
        ts.TraceEvent( TraceEventType.Verbose, 0, builder.ToString() )

    let private setProcedure(command : IDbCommand) (procedureName : String) ([<ParamArray>] parameters : IDbDataParameter array) =        
        log procedureName parameters
        command.CommandType <- CommandType.StoredProcedure
        command.CommandText <- procedureName
        parameters |> Array.iter (fun p -> command.Parameters.Add(p) |> ignore)
        command              

    let private setText(command : IDbCommand) (sqlText : String) =
        log sqlText [||]
        command.CommandType <- CommandType.Text
        command.CommandText <- sqlText
        command              

    let private openConnection(connectionString : String) =
        let connection = new MySqlConnection(connectionString)
        connection.Open()
        connection

    let executeScalarCommand<'T>(connection : IDbConnection) (setCommand : IDbCommand -> IDbCommand) =
        use command = setCommand(connection.CreateCommand())
        command.ExecuteScalar() :?> 'T

    let executeTextCommand(connection : IDbConnection) (setCommand : IDbCommand -> IDbCommand) =
        use command = setCommand(connection.CreateCommand())        
        let dataSet = new DataSet()
        let adapter = new MySqlDataAdapter(command :?> MySqlCommand)
        adapter.Fill(dataSet), dataSet

    let executeScalar<'T>(connection : IDbConnection) (procedureName : String) ([<ParamArray>] parameters : IDbDataParameter array)=
        let commandFn = fun (command : IDbCommand) -> setProcedure command procedureName parameters
        executeScalarCommand<'T> connection commandFn

    let executeText<'T>(connection : IDbConnection) (sqlText : String) (convert : DataRow -> 'T) =        
        let commandFn = fun (command : IDbCommand) -> setText command sqlText 
        let rows, dataSet = executeTextCommand connection commandFn
        match rows, dataSet with
        | r, d when r = 0 || d = null || d.Tables.Count = 0 -> []
        | r, d -> d.Tables.[0].Rows |> Seq.cast<DataRow> |> Seq.map convert |> Seq.toList

    let getMetadata (connectionString : string) =
        let sqlText = 
            """SELECT d.dimension_id, d.name dimension_name, m.measure_id, m.value measure_value
                 FROM dimensions d INNER JOIN measures m ON d.dimension_id = m.dimension_id
                ORDER BY d.dimension_id, m.value;"""
        let convert(row : DataRow) =
            let dimension = { Id = Convert.ToUInt64(row.["dimension_id"]); Name = row.["name"].ToString() }
            let value = row.["measure_value"].ToString()
            dimension, value
        use connection = openConnection connectionString
        executeText<Dimension * String> connection sqlText convert
        
    let getDimensions (connectionString : string) =
        let toDimension(row : DataRow) = { Id = Convert.ToUInt64(row.["dimension_id"]); Name = row.["name"].ToString() }
        use connection = openConnection connectionString
        executeText<Dimension> connection "SELECT dimension_id, name FROM dimensions ORDER BY ordinal, dimension_id" toDimension

    let addDimension (connectionString : String) (dimensionName : String) =
        let parameters : IDbDataParameter[] = [| new MySqlParameter("inName", dimensionName) |]
        use connection = openConnection connectionString
        executeScalar<uint64> connection "sp_dimensions_merge" parameters

    let removeDimension (connectionString : String) (dimensionId : uint64) =
        let sqlText = String.Format("DELETE FROM dimensions WHERE dimension_id = '{0}'", dimensionId)
        use connection = openConnection connectionString
        executeText<unit> connection sqlText (fun r -> ignore()) |> ignore
        
    let addMeasure (connectionString : String) (``measure`` : Measure) =
        let parameters : IDbDataParameter[] = 
            [| new MySqlParameter("inDimensionId", ``measure``.Dimension.Id); 
               new MySqlParameter("inValue", ``measure``.Value);
               new MySqlParameter("inDeprecated", false);
               new MySqlParameter("inDescription", "");
            |]
        use connection = openConnection connectionString
        executeScalar<uint64> connection "sp_measures_merge" parameters

    let removeMeasure (connectionString : String) (measureId : uint64) =
        let sqlText = String.Format("DELETE FROM measures WHERE measure_id = '{0}'", measureId)
        use connection = openConnection connectionString
        executeText<unit> connection sqlText (fun r -> ignore()) |> ignore

    let addProperty (connectionString : String) (``measure`` : Measure) (property : Property) =
        let parameters : IDbDataParameter[] = 
            [| new MySqlParameter("inDimensionId", ``measure``.Dimension.Id); 
               new MySqlParameter("inValue", ``measure``.Value);
               new MySqlParameter("inDeprecated", property.Deprecated);
               new MySqlParameter("inDescription", property.Description);
            |]
        use connection = openConnection connectionString
        executeScalar<uint64> connection "sp_measures_merge" parameters

    let putProperty (connectionString : String) (measureId : uint64) (property : Property) =
        let parameters : IDbDataParameter[] = 
            [| new MySqlParameter("inMeasureId", measureId); 
               new MySqlParameter("inTimestamp", property.Timestamp); 
               new MySqlParameter("inPropertyJson", JsonConvert.SerializeObject(property))
            |]
        use connection = openConnection connectionString
        executeScalar<uint64> connection "sp_properties_merge" parameters |> ignore       
        

type MySqlStore(connectionString : String) =
    static let ts = new TraceSource( "Myriad.Store", SourceLevels.Information )
    let cache = new MyriadCache()    // Kill
    let store = new MyriadStore()    // Kill

    let measureIds = new ConcurrentDictionary<Measure, uint64>()

    let addMeasure (``measure`` : Measure) =
        let measureId = MySqlAccessor.addMeasure connectionString ``measure`` 
        measureIds.[``measure``] <- measureId
        measureId

    let addProperty (``measure`` : Measure) (property : Property) =
        MySqlAccessor.addProperty connectionString ``measure`` property

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
        ts.TraceEvent(TraceEventType.Information, 0, "Initializing MySql store.")
        // TODO : Initialize db

        //let dimensions = MySqlAccessor.getDimensions connectionString
        //dimensions |> List.iter (fun d -> store.PutDimension d |> ignore)      
        

        // TODO: Initialize internal cache from MySql
        ignore()
        
    member x.GetMetadata() =         
        let values = MySqlAccessor.getMetadata connectionString
        let map = new Dictionary<Dimension, List<String>>()
        let add(pair : Dimension * String) =            
            let success, values = map.TryGetValue(fst(pair))
            match success with
            | false -> map.[fst(pair)] <- new List<String>([snd(pair)])
            | true -> values.Add(snd(pair))                
        values |> Seq.iter add

        map
        |> Seq.map (fun kv -> { Dimension = kv.Key; Values = kv.Value |> Seq.toArray } ) 
        |> Seq.toList        

    member x.GetDimensions() = MySqlAccessor.getDimensions connectionString 

    member x.GetDimension(dimensionName : String) = store.GetDimension dimensionName        

    member x.AddDimension(dimensionName : String) = 
        let newId(name : String) = MySqlAccessor.addDimension connectionString name
        store.AddDimension dimensionName newId
        
    member x.RemoveDimension(dimension : Dimension) = 
        MySqlAccessor.removeDimension connectionString dimension.Id
        store.RemoveDimension dimension

    member x.AddMeasure(``measure`` : Measure) = 
        addMeasure ``measure`` |> ignore
        store.AddMeasure ``measure``

    member x.RemoveMeasure(``measure`` : Measure) = 
        let success, removed = measureIds.TryRemove(``measure``)
        if success then
            MySqlAccessor.removeMeasure connectionString removed
        store.RemoveMeasure ``measure`` 
    
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
        x.UpdateMeasures property
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
        x.UpdateMeasures property        
        property

    member private x.UpdateMeasures (property : Property) =
        let propertyMeasure = { Dimension = store.PropertyDimension; Value = property.Key }
        let propertyId = addProperty propertyMeasure property
        MySqlAccessor.putProperty connectionString propertyId property

        let measureSet = store.UpdateMeasures property
        measureSet |> Set.iter (fun m -> addMeasure m |> ignore)

        
