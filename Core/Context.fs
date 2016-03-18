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

    override x.ToString() = 
        let measures = x.Measures |> Seq.map (fun m -> m.Dimension.Name + " = [" + m.Value + "]")
        String.Format("AsOf: {0} Measures: {1}", Epoch.FormatDateTimeOffset(x.AsOf), String.Join(", ", measures))

    static member Latest with get() = { AsOf = DateTimeOffset.MaxValue; Measures = Set.empty }
