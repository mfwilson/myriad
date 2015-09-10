# myriad
Configuration as a Service

## Why?
Inspired by this article:
https://www.devopsonwindows.com/implement-context-aware-key-value-store/

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