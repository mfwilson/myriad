﻿namespace Myriad.Store

open System
open System.Data
open System.Diagnostics
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
        ts.TraceEvent( TraceEventType.Verbose, 0, sqlText )
        command.CommandType <- CommandType.Text
        command.CommandText <- sqlText
        command              

    let private openConnection(connectionString : String) =
        let connection = new MySqlConnection(connectionString)
        connection.Open()
        connection

    let executeNonQueryCommand(connection : IDbConnection) (setCommand : IDbCommand -> IDbCommand) =
        use command = setCommand(connection.CreateCommand())
        command.ExecuteNonQuery()        

    let executeScalarCommand<'T>(connection : IDbConnection) (setCommand : IDbCommand -> IDbCommand) =
        use command = setCommand(connection.CreateCommand())
        command.ExecuteScalar() :?> 'T

    let executeTextCommand(connection : IDbConnection) (setCommand : IDbCommand -> IDbCommand) =
        use command = setCommand(connection.CreateCommand())        
        let dataSet = new DataSet()
        let adapter = new MySqlDataAdapter(command :?> MySqlCommand)
        adapter.Fill(dataSet), dataSet

    let executeNonQuery(connection : IDbConnection) (sqlText : String) =        
        let commandFn = fun (command : IDbCommand) -> setText command sqlText 
        executeNonQueryCommand connection commandFn

    let executeScalar<'T>(connection : IDbConnection) (procedureName : String) ([<ParamArray>] parameters : IDbDataParameter array)=
        let commandFn = fun (command : IDbCommand) -> setProcedure command procedureName parameters
        executeScalarCommand<'T> connection commandFn

    let executeText<'T>(connection : IDbConnection) (sqlText : String) (convert : DataRow -> 'T) =        
        let commandFn = fun (command : IDbCommand) -> setText command sqlText 
        let rows, dataSet = executeTextCommand connection commandFn
        match rows, dataSet with
        | r, d when r = 0 || d = null || d.Tables.Count = 0 -> []
        | r, d -> d.Tables.[0].Rows |> Seq.cast<DataRow> |> Seq.map convert |> Seq.toList

    let private toDimension(row : DataRow) = { Id = Convert.ToUInt64(row.["dimension_id"]); Name = row.["name"].ToString() }

    let getMetadata (connectionString : string) =
        let sqlText = 
            """SELECT d.dimension_id, d.name dimension_name, m.measure_id, m.value measure_value
                 FROM dimensions d INNER JOIN measures m ON d.dimension_id = m.dimension_id
                ORDER BY d.dimension_id, m.value;"""
        let convert(row : DataRow) = toDimension row, row.["measure_value"].ToString() 
        use connection = openConnection connectionString
        executeText<Dimension * String> connection sqlText convert
        
    let getDimensions (connectionString : string) =
        use connection = openConnection connectionString
        executeText<Dimension> connection "SELECT dimension_id, name FROM dimensions ORDER BY ordinal, dimension_id" toDimension

    let getDimension (connectionString : string) (dimensionName : String) =
        let sqlText = String.Format("SELECT dimension_id, name dimensions WHERE name = '{0}'", dimensionName)
        use connection = openConnection connectionString
        executeText<Dimension> connection sqlText toDimension |> List.tryPick Some        

    let getDimensionValues (connectionString : string) (dimensionId : uint64) =
        let sqlText = String.Format("SELECT value FROM measures WHERE dimension_id = '{0}' ORDER BY value", dimensionId)
        use connection = openConnection connectionString
        executeText<String> connection sqlText (fun r -> r.["value"].ToString())

    let addDimension (connectionString : String) (dimensionName : String) =
        let parameters : IDbDataParameter[] = [| new MySqlParameter("inName", dimensionName) |]
        use connection = openConnection connectionString
        executeScalar<uint64> connection "sp_dimensions_merge" parameters

    let removeDimension (connectionString : String) (dimensionId : uint64) =
        let sqlText = String.Format("DELETE FROM dimensions WHERE dimension_id = '{0}'", dimensionId)
        use connection = openConnection connectionString
        executeNonQuery connection sqlText 
        
    let addMeasure (connectionString : String) (``measure`` : Measure) =
        let parameters : IDbDataParameter[] = 
            [| new MySqlParameter("inDimensionId", ``measure``.Dimension.Id); 
               new MySqlParameter("inValue", ``measure``.Value);
               new MySqlParameter("inDeprecated", false);
               new MySqlParameter("inDescription", "");
            |]
        use connection = openConnection connectionString
        executeScalar<uint64> connection "sp_measures_merge" parameters

    let removeMeasure (connectionString : String) (``measure`` : Measure) =
        let sqlText = String.Format("DELETE FROM measures WHERE dimension_id = '{0}' AND value = '{1}'", ``measure``.Dimension.Id, ``measure``.Value)
        use connection = openConnection connectionString
        executeNonQuery connection sqlText 

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

    let setProperty (connectionString : String) (property : Property) =
        
        // overwrite existing property (instead of merge)
        property

    let addOrUpdateProperty (connectionString : String) (propertyKey : String) (add : String -> Property) (update : string -> Property -> Property) = 
        
        use connection = openConnection connectionString
        let txn = connection.BeginTransaction()
        try
            // Select latest from DB
            // If nothing exists, call add else call update
            let property = add(propertyKey)

            // Write property to DB

            txn.Commit()
            Some(property)
        with
        | ex -> txn.Rollback()
                ts.TraceEvent(TraceEventType.Error, 0, ex.ToString())
                None