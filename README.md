# myriad
Configuration as a Service (CaaS)
"having innumerable aspects or elements"

## Build Status
|  |  Status of last build |
| :------ | :------: |
| **Mono** | [![Travis build status](https://api.travis-ci.org/mfwilson/myriad.svg?branch=master)](https://travis-ci.org/mfwilson/myriad) |
| **Windows** | [![AppVeyor Build status](https://ci.appveyor.com/api/projects/status/df7nicjda1av5lim?svg=true)](https://ci.appveyor.com/project/mfwilson/myriad) |

## Why?
Inspired by this article:
https://www.devopsonwindows.com/implement-context-aware-key-value-store/

Context-aware key-value stores (CA-KVS) can be critical to operational stability in environments where there may be a high cost to deploying software and managing multiple system components with shared configuration. CA-KVS seeks to decrease overall system complexity by centralizing all configuration in one place. Applications provide a context and key name and the service provides a value specific to that context.

### Terminology
- **Context-aware key-value stores (CA-KVS or store):** Repository where configuration data is stored
- **Dimension:** Defines a scope of context. This is a named value that describes a single extent of the configuration space, e.g. "Environment" or "Application". 
- **Measure:** Defines a value for a dimension; e.g. Dimension(Environment) = "Production" 
- **Cluster:** Defines a set of measures and value; e.g. Dimension(Environment) = "Production" and Dimension(Application) = "MyApp" have the value "foo". 
- **Property:** Defines a key and ordered list of clusters. Clusters are ordered by weight as described below.
- **Context:** Defines a point in time (as of) and set of measures. The context is matched against a property in order to extract a value from one of the clusters. Note that any or none of the clusters could match a context and only the first is returned when resolving a property value. 
 
Generally, context defines the degrees of freedom over which configuration key-values are needed. The ordered list of all dimensions defines the supported context for a given store. In a single application space, for example, there may only be a distinction between environments, like "production" and "development". For multiple components, another dimension like "Application" may be required.  

## Approach

1. Take dimensions and determine weights based on ordering
   - Dimensions are weighted in reverse order, e.g. Instance, Application, Location, Environment
   - Weights are computed for each level as 2^n where n is the level,
     e.g. Environment=1, Location=2, Application=4, Instance=8
   - When no dimensions are specified, the key gets a weight of zero (effectively the value's default)
2. For a given property, apply the weighted ordering to its set of clusters, sorting from greatest to least

	| Environment (1) | Location (2) | Application (4) | Instance (8) |  Total Weight | Property  Value |
	|:---------------:|:------------:|:---------------:|:------------:|:-------------:|:---------------:|
	|        x        |       x      |        -        |       x      |            11 | apple           |
	|        x        |       -      |        -        |       x      |             9 | pear            |
	|        -        |       -      |        -        |       x      |             8 | pecan           |
	|        -        |       -      |        x        |       -      |             4 | peach           |
	|        -        |       x      |        -        |       -      |             2 | strawberry      |
	|        x        |       -      |        -        |       -      |             1 | apricot         |
	|        -        |       -      |        -        |       -      |             0 | pumpkin         |

3. Lookups are accomplished by, for a given key, matching the provided context against the set for the first
   positive match; matches must match over all dimensions to be considered equal
   
## System Details

### Platforms
Myriad runs on Windows and Linux under Mono and can be hosted in Amazon Web Services. Support for Azure is planned.

### Persistence
Myriad supports the following persistent stores:
- MySql
- Redis (in progress)
- Microsoft SQL Server (planned)
