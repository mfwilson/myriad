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

    let logMessage (level : LogLevel) (getLine : LogLevel -> Message) =
        async {
            let logLevel = getNLogLevel level
            let message = getLine level
            logger.Log(logLevel, message)
        } 

    interface Logger with
        member x.name with get() = [| "Suave2NLogAdapter" |]
        member x.log level getLine = logMessage level getLine
        member x.logWithAck level getLine = logMessage level getLine
