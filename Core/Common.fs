namespace Myriad

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.Linq
open System.Reflection
open System.Runtime.CompilerServices

type Epoch =
    static member Value = DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)

    /// Microseconds since the epoch 
    static member EpochMicroseconds = Epoch.Value.Ticks / 10L
    
    /// Get the offset from the epoch in microseconds
    static member GetOffset(utcTicks) = (utcTicks / 10L - Epoch.EpochMicroseconds) 

    static member UtcNow with get() = Epoch.GetOffset(DateTimeOffset.UtcNow.Ticks)

    static member ToDateTimeOffset(epochOffsetMicroseconds : Int64) = 
        DateTimeOffset( (epochOffsetMicroseconds + Epoch.EpochMicroseconds) * 10L, TimeSpan.Zero )

    static member FormatDateTimeOffset(value : DateTimeOffset) =
        value.ToString("yyyy-MM-dd HH:mm:ss.ffffff")

[<Extension>]
type AttributeExtensions() =

    [<Extension>]
    static member GetAttribute<'A when 'A :> Attribute>(provider : ICustomAttributeProvider, ``inherit`` : bool) =
        if provider = null then
            Unchecked.defaultof<'A>
        else
            let attributes = provider.GetCustomAttributes(typeof<'A>, ``inherit``).Cast<'A>()
            attributes.DefaultIfEmpty(Unchecked.defaultof<'A>).First<'A>()
                
    [<Extension>]
    static member GetAttribute<'A when 'A :> Attribute>(provider : ICustomAttributeProvider) =
        AttributeExtensions.GetAttribute<'A>(provider, false)

module CurrentProcess =
    let Process = Process.GetCurrentProcess()

    let EntryAssembly =
        try
            Assembly.GetEntryAssembly()
        with
        | _ -> AppDomain.CurrentDomain.GetAssemblies().Single(fun assembly -> assembly.Location.Equals(Process.MainModule.FileName, StringComparison.OrdinalIgnoreCase))

    let private Values = new ConcurrentDictionary<String, String>()

    let private GetValue<'T when 'T : equality and 'T : null>(attribute : 'T, extractFn : 'T -> String, defaultValue : String) =
        if attribute = null then 
            defaultValue
        else
            let value = extractFn(attribute)
            if String.IsNullOrEmpty(value) then defaultValue else value

    let private GetAssemblyValue<'T when 'T : equality and 'T : null and 'T :> Attribute>(assembly : ICustomAttributeProvider, extractFn : 'T -> String, defaultValue : String) =
        let attribute = assembly.GetAttribute<'T>()
        GetValue(attribute, extractFn, defaultValue)

    let GetTitle() = Values.GetOrAdd("Title", fun _ -> GetAssemblyValue<AssemblyTitleAttribute>(EntryAssembly, (fun a -> a.Title), "") )

    let GetVersion() = if EntryAssembly <> null then EntryAssembly.GetName().Version else new Version()       

    let GetVersionAsString() = Values.GetOrAdd("Version", fun _ -> GetVersion().ToString() )

    let GetProcessId() = Process.Id
