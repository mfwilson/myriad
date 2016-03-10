namespace Myriad

open System
open System.Runtime.Serialization
open System.Xml
open System.Xml.Serialization

type MyriadProperty = 
    { Name : String; Value : String; Deprecated : bool }

    interface IXmlSerializable with
        member x.GetSchema() = null
        member x.ReadXml(reader) = ignore()
        member x.WriteXml(writer) = x.WriteXml(writer)

    override x.ToString() = 
        let deprecated = if x.Deprecated then "(Deprecated)" else ""
        String.Format("[{0}] = [{1}] {2}", x.Name, x.Value, deprecated)

    member x.WriteXml(writer : XmlWriter) =         
        writer.WriteStartElement("Property")
        writer.WriteAttributeString("Name", x.Name)
        writer.WriteAttributeString("Deprecated", x.Deprecated.ToString())
        writer.WriteCData(x.Value)
        writer.WriteEndElement()

/// Response data from a get
[<CLIMutable>]
type MyriadGetResponse =
    { Requested : DateTimeOffset; Context : Context; Properties : MyriadProperty seq }
    
    interface IXmlSerializable with
        member x.GetSchema() = null
        member x.ReadXml(reader) = ignore()
        member x.WriteXml(writer) = x.WriteXml(writer)
                        
    member x.WriteXml(writer : XmlWriter) =             
        writer.WriteAttributeString("Requested", x.Requested.ToString("yyyy-MM-ddTHH:mm:ss.fff"))
        x.Context.WriteXml(writer)
        writer.WriteStartElement("Properties")
        x.Properties |> Seq.iter (fun p -> p.WriteXml(writer))
        writer.WriteEndElement()

/// Response data from a query
type MyriadQueryResponse =
    { data : Map<String, String> seq }

/// Response data from a query
[<CLIMutable>]
type MyriadGetPropertyResponse =
    { Requested : DateTimeOffset; Properties : Property seq }

    interface IXmlSerializable with
        member x.GetSchema() = null
        member x.ReadXml(reader) = ignore()
        member x.WriteXml(writer) = x.WriteXml(writer)
                        
    member x.WriteXml(writer : XmlWriter) =             
        writer.WriteAttributeString("Requested", x.Requested.ToString("yyyy-MM-ddTHH:mm:ss.fff"))
        writer.WriteStartElement("Properties")
        x.Properties |> Seq.iter (fun p -> p.WriteXml(writer))
        writer.WriteEndElement()

/// Response data from a property set
[<CLIMutable>]
type MyriadSetPropertyResponse =
    { Requested : DateTimeOffset; Property : Property }
    
    interface IXmlSerializable with
        member x.GetSchema() = null
        member x.ReadXml(reader) = ignore()
        member x.WriteXml(writer) = x.WriteXml(writer)
                        
    member x.WriteXml(writer : XmlWriter) =  
        writer.WriteAttributeString("Requested", x.Requested.ToString("yyyy-MM-ddTHH:mm:ss.fff"))
        x.Property.WriteXml(writer)
