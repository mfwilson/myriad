namespace Myriad.Web

open System
open System.Collections.Generic
open System.Configuration
open System.Diagnostics
open System.Reflection

open NLog

open Myriad
open Myriad.Store

module AppConfiguration =
    let private logger = LogManager.GetCurrentClassLogger()    

    let private (|InvariantEqual|_|) (str:string) arg = if String.Compare(str, arg, StringComparison.InvariantCultureIgnoreCase) = 0 then Some() else None

    let private getArgumentValue (arguments : (String * String) seq) (flags : String list) =        
        let compare a b =
            match a with
            | InvariantEqual b -> true
            | _ -> false            

        let pair = arguments |> Seq.tryFind (fun a -> flags |> List.exists (fun f -> compare (fst a) f))
        if pair.IsNone then None else Some (snd pair.Value)

    let private applyAppConfig (values : Dictionary<String, String>) =
        let addValue (key : String) = 
            try                
                values.[key] <- ConfigurationManager.AppSettings.[key]                
            with
            | ex -> logger.Error(ex, "Unable to set key: '{0}'", key)
        ConfigurationManager.AppSettings.AllKeys |> Seq.iter addValue 

    let private applyCommandLine (values : Dictionary<String, String>) =        
        let arguments = Environment.GetCommandLineArgs().[1..] |> Seq.pairwise 

        let keys = [ "port", ["--port"; "-p"]; 
                     "prefix", ["--prefix"; "-#"]; 
                     "storeType", ["--store-type"; "-s"] 
                     "history", ["--history"; "-h"] ]                   
        keys 
        |> List.map (fun k -> fst k, getArgumentValue arguments (snd k)) 
        |> List.filter (fun kv -> (snd kv).IsSome)
        |> List.iter (fun kv -> values.[fst kv] <- (snd kv).Value)

    let private configurationMap =
        let values = new Dictionary<String, String>(StringComparer.InvariantCultureIgnoreCase)
        applyAppConfig values
        applyCommandLine values        
        values

    let private getValue<'T>(key : String) (defaultValue : 'T) (convert : String -> 'T) =
        try
            let success, result = configurationMap.TryGetValue(key)
            if not success then defaultValue else convert(result)
        with
        | _ -> defaultValue

    let getPort() = getValue "port" 7888us (Convert.ToUInt16)

    let getPrefix() = getValue "prefix" "/api/1/myriad/" id

    let getHistory() =        
        let timespan = getValue "history" TimeSpan.Zero (TimeSpan.Parse)            
        if timespan = TimeSpan.Zero then MyriadHistory.All() else MyriadHistory.TimeAndLatest(timespan)

    let getEngine() =        
        let defaultStoreType = "Myriad.Store.MemoryStore, Myriad.Store, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
        let typeString = getValue "storeType" defaultStoreType id
        let storeType = Type.GetType(typeString)
        if storeType = null then raise(TypeLoadException("Cannot create data store type: " + typeString))
        let store = Activator.CreateInstance(storeType) :?> IMyriadStore        
        let history = getHistory()
        MyriadEngine(store, history)

        

        