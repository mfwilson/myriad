namespace Myriad

open System
open System.Runtime.Serialization
open System.Xml
open System.Xml.Serialization

type MyriadProperty = 
    { Name : String; Value : String }

    interface IXmlSerializable with
        member x.GetSchema() = null
        member x.ReadXml(reader) = ignore()
        member x.WriteXml(writer) = x.WriteXml(writer)

    member x.WriteXml(writer : XmlWriter) =         
        writer.WriteStartElement("Property")
        writer.WriteAttributeString("Name", x.Name)
        writer.WriteCData(x.Value)
        writer.WriteEndElement()

/// Response data from a configuration query
[<CLIMutable>]
type MyriadResponse =
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
