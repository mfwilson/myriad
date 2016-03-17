namespace Myriad.Store

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Data
open System.Diagnostics
open System.Runtime.InteropServices
open System.Text

open Myriad        

type MySqlStore(connectionString : String) =
    static let ts = new TraceSource( "Myriad.Store", SourceLevels.Information )
    let cache = new MyriadCache()    // Kill

    let addMeasure (``measure`` : Measure) =
        MySqlAccessor.addMeasure connectionString ``measure`` 

    let addProperty (``measure`` : Measure) (property : Property) =
        MySqlAccessor.addProperty connectionString ``measure`` property

    let propertyDimension = 
        let dimension = MySqlAccessor.getDimension connectionString "Property"
        match dimension with
        | Some d -> d
        | None -> raise(new Exception("Check database setup, cannot find Property dimension.")) 

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

    member x.GetDimension(dimensionName : String) = MySqlAccessor.getDimension connectionString dimensionName
        
    member x.AddDimension(dimensionName : String) = 
        let dimensionId = MySqlAccessor.addDimension connectionString dimensionName 
        { Id = dimensionId; Name = dimensionName }
                
    member x.RemoveDimension(dimension : Dimension) = 
        MySqlAccessor.removeDimension connectionString dimension.Id > 0

    member x.AddMeasure(``measure`` : Measure) =
        addMeasure ``measure`` |> ignore
        let values = MySqlAccessor.getDimensionValues connectionString ``measure``.Dimension.Id
        Some({ Dimension = ``measure``.Dimension; Values = values |> List.toArray })

    member x.RemoveMeasure(``measure`` : Measure) = 
        MySqlAccessor.removeMeasure connectionString ``measure`` > 0
    
    member x.SetDimensionOrder(orderedDimensions : Dimension list) =       
        // TODO: Implement dimension ordering
        orderedDimensions
//        let current = store.Dimensions |> Set.ofSeq
//        let proposed = orderedDimensions |> Set.ofList
//        // If this is not the same set, we cannot reorder
//        if current <> proposed then store.Dimensions |> List.ofSeq else setDimensionOrder orderedDimensions                       

    member x.GetProperties(history : MyriadHistory) = 
        // Get latest properties from properties table
        cache.GetProperties() |> Seq.toList        

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
        // Get first property before asOf
        cache.GetProperty(propertyKey, asOf)

    member x.GetMeasureBuilder() = 
        let dimensions = x.GetDimensions()
        let dimensionMap = dimensions |> Seq.map (fun d -> d.Name, d) |> Map.ofSeq
        MeasureBuilder(dimensionMap)

    member x.GetPropertyBuilder() = 
        PropertyBuilder(x.GetDimensions())

    member x.SetProperty(property : Property) =
        x.UpdateMeasures property
        MySqlAccessor.setProperty connectionString property

    member x.PutProperty(value : PropertyOperation) =
        let pb = x.GetPropertyBuilder()
        let filter = [ propertyDimension ]
        
        let add (key : string) = 
            value.ToProperty(pb.OrderClusters, filter)
            
        let update (key : string) (currentProperty : Property) = 
            let filterMeasures(cluster) = PropertyOperation.FilterMeasures(cluster, filter)
            let applyOperations(current : Cluster list) (operation : Operation<Cluster>) =
                match operation with
                | Add(cluster) -> filterMeasures(cluster) :: current
                | Update(previous, updated) -> filterMeasures(updated) :: (current |> List.filter (fun c -> c <> previous))
                | Remove(cluster) -> current |> List.filter (fun c -> c <> cluster)
            let clusters = pb.OrderClusters (value.Operations |> List.fold applyOperations currentProperty.Clusters) 
            Property.Create(currentProperty.Key, value.Description, value.Deprecated, value.Timestamp, clusters)       

        let property = MySqlAccessor.addOrUpdateProperty connectionString value.Key add update
        if property.IsNone then raise(new Exception("Unable to put property to database."))
        x.UpdateMeasures property.Value        
        property.Value

    member private x.UpdateMeasures (property : Property) =
        let propertyMeasure = { Dimension = propertyDimension; Value = property.Key }
        let propertyId = addProperty propertyMeasure property
        MySqlAccessor.putProperty connectionString propertyId property

        let measures = property.Clusters |> List.map (fun c -> c.Measures) |> Set.unionMany 
        measures |> Set.iter (fun m -> MySqlAccessor.addMeasure connectionString m |> ignore)

        
(*
-- --------------------------------------------------------
-- Host:                         127.0.0.1
-- Server version:               10.0.20-MariaDB - mariadb.org binary distribution
-- Server OS:                    Win64
-- HeidiSQL Version:             9.3.0.4984
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;

-- Dumping database structure for configuration
CREATE DATABASE IF NOT EXISTS `configuration` /*!40100 DEFAULT CHARACTER SET utf8 */;
USE `configuration`;


-- Dumping structure for table configuration.dimensions
DROP TABLE IF EXISTS `dimensions`;
CREATE TABLE IF NOT EXISTS `dimensions` (
  `dimension_id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `name` varchar(50) NOT NULL DEFAULT '0',
  `ordinal` smallint(5) NOT NULL DEFAULT '0',
  PRIMARY KEY (`dimension_id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.


-- Dumping structure for table configuration.measures
DROP TABLE IF EXISTS `measures`;
CREATE TABLE IF NOT EXISTS `measures` (
  `measure_id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `dimension_id` bigint(20) unsigned NOT NULL DEFAULT '0',
  `value` varchar(255) NOT NULL DEFAULT '0',
  `deprecated` bit(1) NOT NULL DEFAULT b'0',
  `description` text NOT NULL,
  PRIMARY KEY (`measure_id`),
  UNIQUE KEY `dimension_id_value` (`dimension_id`,`value`),
  CONSTRAINT `FK_measures_dimensions` FOREIGN KEY (`dimension_id`) REFERENCES `dimensions` (`dimension_id`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.


-- Dumping structure for table configuration.properties
DROP TABLE IF EXISTS `properties`;
CREATE TABLE IF NOT EXISTS `properties` (
  `measure_id` bigint(20) unsigned NOT NULL,
  `timestamp` bigint(20) NOT NULL,
  `propertyJson` mediumtext NOT NULL,
  KEY `FK_properties_measures` (`measure_id`),
  KEY `measure_id_timestamp` (`measure_id`,`timestamp`),
  CONSTRAINT `FK_properties_measures` FOREIGN KEY (`measure_id`) REFERENCES `measures` (`measure_id`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.


-- Dumping structure for procedure configuration.sp_dimensions_merge
DROP PROCEDURE IF EXISTS `sp_dimensions_merge`;
DELIMITER //
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_dimensions_merge`(IN `inName` varchar(50))
BEGIN
    SET @dimension_id := 0;

	IF (SELECT @dimension_id := dimension_id FROM dimensions WHERE name = inName) THEN
		BEGIN		
            UPDATE dimensions SET name = inName WHERE name = inName;
			SELECT @dimension_id dimension_id, inName `name`, 0 `ordinal`;
		END;
		ELSE
		BEGIN
			INSERT INTO dimensions ( `name` ) VALUES ( inName );			
			SELECT LAST_INSERT_ID() dimension_id, inName `name`, -1 `ordinal`;
		END;
	END IF;
END//
DELIMITER ;


-- Dumping structure for procedure configuration.sp_measures_merge
DROP PROCEDURE IF EXISTS `sp_measures_merge`;
DELIMITER //
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_measures_merge`(
  IN `inDimensionId` BIGINT unsigned, 
  IN `inValue` varchar(255),
  IN `inDeprecated` BIT(1),
  IN `inDescription` TEXT)
BEGIN
    SET @measure_id := 0;

	IF (SELECT @measure_id := measure_id FROM measures WHERE dimension_id = inDimensionId AND value = inValue) THEN
		BEGIN		
            UPDATE measures 
               SET value = inValue,
                   deprecated = inDeprecated,
                   description = inDescription 
             WHERE measure_id = @measure_id;
             
			SELECT @measure_id measure_id, inDimensionId dimension_id, inValue value, inDeprecated deprecated, inDescription description;
		END;
		ELSE
		BEGIN
			INSERT INTO measures ( dimension_id, value, deprecated, description ) 
                          VALUES ( inDimensionId, inValue, inDeprecated, inDescription );

			SELECT LAST_INSERT_ID() measure_id, inDimensionId dimension_id, inValue value, inDeprecated deprecated, inDescription description;
		END;
	END IF;
END//
DELIMITER ;


-- Dumping structure for procedure configuration.sp_properties_merge
DROP PROCEDURE IF EXISTS `sp_properties_merge`;
DELIMITER //
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_properties_merge`(
  IN inMeasureId BIGINT(20) UNSIGNED,
  IN inTimestamp BIGINT(20) UNSIGNED,
  IN inPropertyJson MEDIUMTEXT)
BEGIN
    SET @measure_id := 0;

	IF (SELECT @measure_id := measure_id FROM properties WHERE measure_id = inMeasureId AND `timestamp` = inTimestamp) THEN
		BEGIN		
            UPDATE properties
               SET propertyJson = inPropertyJson
             WHERE measure_id = inMeasureId AND `timestamp` = inTimestamp;
             
			SELECT @measure_id measure_id, inTimestamp `timestamp`, inPropertyJson propertyJson;
		END;
		ELSE
		BEGIN
			INSERT INTO properties ( `measure_id`, `timestamp`, `propertyJson` ) 
                   VALUES ( inMeasureId, inTimestamp, inPropertyJson );

			SELECT LAST_INSERT_ID() measure_id, inTimestamp `timestamp`, inPropertyJson propertyJson;
		END;
	END IF;    
END//
DELIMITER ;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
*)