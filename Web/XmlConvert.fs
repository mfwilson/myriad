namespace Myriad.Web

open System
open System.IO
open System.Runtime.Serialization
open System.Text
open System.Xml
open System.Xml.Linq
open System.Xml.Serialization

open Myriad

open Microsoft.FSharp.Collections

module XmlConvert =
    
    let private xmlNs = "myriad.net"

    let private XNameStd name = XName.Get(name, xmlNs)

    let private xAttribute name value = XAttribute(XName.Get name, value) :> XObject

    let private xElement name (value:obj) = XElement(XName.Get name, value) :> XObject

    let private createDimensionElement (dimension : Dimension) =
        let attributes = [| xAttribute "Id" (dimension.Id.ToString()); xAttribute "Name" dimension.Name |]                                 
        XElement(XNameStd "Dimension", attributes)
        
    let private createValuesElement (values : String array) =
        let elements = values |> Array.map (fun v -> XElement(XNameStd "Value", v))
        //let element = XElement(XNameStd "Values")
        //values |> Array.iter (fun v -> element.Add(XElement(XNameStd "Value", v)))
        XElement(XNameStd "Values", elements)

    let private createDimensionValuesElement (dimensionValues : DimensionValues) =        
        let elements = [| createDimensionElement dimensionValues.Dimension; createValuesElement dimensionValues.Values |]        
        XElement(XNameStd "DimensionValues", elements)                     

    let rec private serializeItems (parent : XElement) (collection : Object seq) =
        let serializeItem (item : Object) =
            match item with
            | :? DimensionValues as dimensionValues -> parent.Add(createDimensionValuesElement dimensionValues)
            | :? (Object seq) as children -> let child = XElement(XNameStd "Items")
                                             parent.Add child
                                             serializeItems child children
            | _ -> parent.Add(XElement(XNameStd "Item", item.ToString()))

        collection |> Seq.iter serializeItem

    let private serializeList(collection : Object seq) =
        let doc = XDocument(XElement(XNameStd "Root"))       
        serializeItems doc.Root collection                
        doc.ToString()       

    let private serializeObject (value : Object) =
        let builder = new StringBuilder()
        let xmlSerializer = new XmlSerializer(value.GetType()) 
        xmlSerializer.Serialize(new XmlTextWriter(new StringWriter(builder)), value)
        builder.ToString()

    let SerializeObject(value : Object) =        
        match value with
        | :? (Object seq) as collection -> serializeList collection
        | _ -> serializeObject value


