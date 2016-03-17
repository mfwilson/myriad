namespace Myriad.Web

open System
open System.Collections.Generic
open System.Configuration
open System.Diagnostics
open System.Reflection

open Myriad
open Myriad.Store

module AppConfiguration =
    let private ts = new TraceSource( "Myriad.Web", SourceLevels.Information )

    let private configurationMap =
        let values = new Dictionary<String, String>(StringComparer.InvariantCultureIgnoreCase)

        let addValue (key : String) = 
            try                
                values.[key] <- ConfigurationManager.AppSettings.[key]                
            with
            | ex -> ts.TraceEvent(TraceEventType.Error, 0, "Unable to set key: '{0}', exception: {1}",key, ex)
        ConfigurationManager.AppSettings.AllKeys |> Seq.iter addValue        
        values

    let private getValue<'T>(key : String) (defaultValue : 'T) (convert : String -> 'T) =
        let success, result = configurationMap.TryGetValue(key)
        if not success then defaultValue else convert(result)

    let getPort() = getValue "port" 7888us (Convert.ToUInt16)

    let getPrefix() = getValue "prefix" "/api/1/myriad/" id

    let getEngine() =        
        let defaultStoreType = "Myriad.Store.MemoryStore, Myriad.Store, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
        let typeString = getValue "storeType" defaultStoreType id
        let storeType = Type.GetType(typeString)
        if storeType = null then raise(TypeLoadException("Cannot create data store type: " + typeString))
        let store = Activator.CreateInstance(storeType) :?> IMyriadStore        
        MyriadEngine(store)

        

        