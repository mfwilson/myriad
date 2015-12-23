namespace Myriad.Web

open System
open System.IO
open System.Runtime.Serialization
open System.Text
open System.Xml
open System.Xml.Serialization

module XmlConvert =
    
    let SerializeObject(value : Object) =
        let builder = new StringBuilder()
        let xmlSerializer = new XmlSerializer(value.GetType()) 
        xmlSerializer.Serialize(new XmlTextWriter(new StringWriter(builder)), value)
        builder.ToString()
