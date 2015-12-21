namespace Myriad

open System
open System.Xml
open System.Xml.Serialization

type Context = 
    { AsOf : DateTimeOffset; Measures : Set<Measure> }

    interface IXmlSerializable with
        member x.GetSchema() = null
        member x.ReadXml(reader) = ignore()
        member x.WriteXml(writer) = x.WriteXml(writer)
                        
    member x.WriteXml(writer : XmlWriter) = 
        writer.WriteStartElement("Context")
        writer.WriteAttributeString("AsOf", x.AsOf.ToString("yyyy-MM-ddTHH:mm:ss.fff"))
        x.Measures |> Seq.iter (fun m -> m.WriteXml(writer))
        writer.WriteEndElement()