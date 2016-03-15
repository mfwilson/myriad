namespace Myriad.Web

open System

open Suave.Logging

type LoggingAdapter() =
    let logger = NLog.LogManager.GetCurrentClassLogger()

    let getNLogLevel(level : LogLevel) =
        match level with        
        | LogLevel.Info -> NLog.LogLevel.Info
        | LogLevel.Warn -> NLog.LogLevel.Warn
        | LogLevel.Error -> NLog.LogLevel.Error
        | LogLevel.Fatal -> NLog.LogLevel.Fatal
        | LogLevel.Debug -> NLog.LogLevel.Debug
        | LogLevel.Verbose -> NLog.LogLevel.Trace                

    interface Logger with
        member x.Log level fLine = x.Log level fLine

    member x.Log (level : LogLevel) (getLine : unit -> LogLine) =         
        let logLevel = getNLogLevel level
        let logEvent = getLine()        
        logger.Log(logLevel, logEvent.message)

        


