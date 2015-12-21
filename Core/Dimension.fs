namespace Myriad

open System
open System.Xml
open System.Xml.Serialization

type IDimension =
    abstract Id : Int64 with get
    abstract Name : String with get

/// Dimensions are equivalent over their ids
[<CustomEquality;CustomComparison>]
type Dimension =
    { Id : Int64; Name : String }
    
    interface IComparable with
        member x.CompareTo other = 
            match other with 
            | :? Dimension as y -> Dimension.CompareTo(x, y)
            | _ -> invalidArg "other" "cannot compare value of different types" 

    interface IDimension with
        member x.Id with get() = x.Id
        member x.Name with get() = x.Name

    interface IXmlSerializable with
        member x.GetSchema() = null
        member x.ReadXml(reader) = ignore()
        member x.WriteXml(writer) = x.WriteXml(writer)

    override x.ToString() = String.Concat("Dimension [", x.Name, "]")

    override x.Equals(obj) = 
        match obj with
        | :? Dimension as y -> Dimension.CompareTo(x, y) = 0
        | _ -> false

    override x.GetHashCode() = hash(x.Id)

    member x.WriteXml(writer : XmlWriter) =
        writer.WriteStartElement("Dimension")
        writer.WriteAttributeString("Id", x.Id.ToString())
        writer.WriteAttributeString("Name", x.Name)
        writer.WriteEndElement()
    
    static member Create(id : Int64, name : String) = { Id = id; Name = name }

    static member CompareTo(x : Dimension, y : Dimension) = compare (x.Id) (y.Id)

type DimensionAudit = Audit<Dimension>

type DimensionValues = 
    { Dimension : IDimension; Values : String list }

type DimensionList = DimensionValues list
    