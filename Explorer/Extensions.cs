using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Myriad.Explorer
{
    public static class Extensions
    {
        public static HashSet<Measure> GetMeasures(List<DimensionValues> dimensionValues, DataRowView view)
        {
            var measures = new HashSet<Measure>();

            foreach (var dimensionValue in dimensionValues)
            {
                if (dimensionValue.Dimension.Name == "Property")
                    continue;

                var value = view[dimensionValue.Dimension.Name];
                if (value == null)
                    continue;

                var measure = new Measure(dimensionValue.Dimension, value.ToString());
                if( string.IsNullOrEmpty(measure.Value) == false )
                    measures.Add(measure);
            }

            return measures;
        }

        public static PropertyOperation GetDeleteOperation(this Property property, HashSet<Measure> measures)
        {
            var removed = property.Clusters.FirstOrDefault(c => measures.SetEquals(c.Measures));
            if (removed == null)
                return null;

            var cluster = Operation<Cluster>.NewRemove(removed);
            var clusters = new List<Operation<Cluster>> { cluster };
            return PropertyOperation.Create(property.Key, property.Description, property.Deprecated, Epoch.UtcNow, clusters);
        }
    }
}
