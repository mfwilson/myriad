namespace Myriad 

open System
open System.Collections.Generic
open System.Runtime.Serialization
open System.Xml
open System.Xml.Serialization

/// Clusters are equivalent over their measures set
/// contain a value, set of measures, and a username
[<CustomEquality;CustomComparison>]
type Cluster = 
    { Value : String; Measures : Set<Measure>; UserName : String; Timestamp : Int64 }

    override x.Equals(obj) = 
        match obj with
        | :? Cluster as y -> (x.Measures = y.Measures)
        | _ -> false

    override x.GetHashCode() = hash(x.Measures)
    
    override x.ToString() = 
        let measures = String.Join(", ", x.Measures)
        String.Format("[{0}], Measures: {1}", x.Value, measures)

    interface IComparable with
        member x.CompareTo other = 
            match other with 
            | :? Cluster as y -> Cluster.CompareTo(x.Measures, y.Measures)
            | _ -> invalidArg "other" "cannot compare value of different types" 

    interface IXmlSerializable with
        member x.GetSchema() = null
        member x.ReadXml(reader) = ignore()
        member x.WriteXml(writer) = x.WriteXml(writer)
                        
    member x.WriteXml(writer : XmlWriter) =             
        writer.WriteStartElement("Cluster")        
        writer.WriteAttributeString("UserName", x.UserName)
        writer.WriteAttributeString("Timestamp", x.Timestamp.ToString())        
        
        writer.WriteStartElement("Value")
        writer.WriteCData(x.Value)
        writer.WriteEndElement()
        
        x.Measures |> Seq.iter (fun m -> m.WriteXml(writer))
        writer.WriteEndElement()

    static member ToMap(propertyKey : String, cluster : Cluster, dimensions : Dimension seq, ordinal : int) =
        let values = [ 
            "Property", propertyKey; 
            "Value", cluster.Value; 
            "Ordinal", ordinal.ToString();
            "UserName", cluster.UserName;
            "Timestamp", cluster.Timestamp.ToString()
        ]

        let measures = cluster.Measures |> Set.toList |> List.map (fun m -> m.Dimension.Name, m.Value)

        let filterByDimension(dimension : Dimension) =
            not(measures |> Seq.exists (fun m -> fst(m) = dimension.Name))

        let defaults = dimensions |> Seq.filter filterByDimension |> Seq.map (fun d -> d.Name, "") |> Seq.toList
        Map.ofList (List.concat [ defaults; measures; values ])

    static member CompareTo(x : Set<Measure>, y : Set<Measure>) = compare x y

    static member Create(value : String, measures : Set<Measure>, userName : String, timestamp : Int64) =
        { Value = value; Measures = measures; UserName = userName; Timestamp = timestamp }

    static member Create(value : String, measures : Set<Measure>) =
        Cluster.Create(value, measures, Environment.UserName, Epoch.UtcNow)

    static member Create(value : String, measures : HashSet<Measure>) =
        Cluster.Create(value, measures |> Set.ofSeq)

    static member Create(value : String, measures : HashSet<Measure>, userName : String, timestamp : Int64) =
        Cluster.Create(value, measures |> Set.ofSeq, userName, timestamp)
