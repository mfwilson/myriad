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
  `property_json` mediumtext NOT NULL,
  PRIMARY KEY (`measure_id`,`timestamp`),
  KEY `FK_properties_measures` (`measure_id`),
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

