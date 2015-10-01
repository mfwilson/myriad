namespace Myriad

open System

type Epoch =    
    static member Value = DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)
    static member Ticks = Epoch.Value.Ticks
    
    /// Get the offset from the epoch in microseconds
    static member GetOffset(utcTicks) = (utcTicks - Epoch.Ticks) / 10L

type Operation = None = 0 | Create = 1 | Update = 2 | Delete = 3

type IAudit =
    abstract Timestamp : Int64 with get
    abstract UserName : String with get
    abstract Operation : Operation with get

type Audit =
    { Timestamp : Int64; UserName : String; Operation : Operation }
    
    interface IAudit with
        member x.Timestamp with get() = x.Timestamp
        member x.UserName with get() = x.UserName
        member x.Operation with get() = x.Operation

    static member Create(timestamp : Int64, userName : String, operation : Operation) =
        { Timestamp = timestamp; UserName = userName; Operation = operation }

type Audit<'T> =
    { Value : 'T 
      Timestamp : Int64
      UserName : String
      Operation : Operation    
    }
    
    interface IAudit with
        member x.Timestamp with get() = x.Timestamp
        member x.UserName with get() = x.UserName
        member x.Operation with get() = x.Operation

    static member Create(value : 'T, timestamp : Int64, userName : String, operation : Operation) =
        { Value = value; Timestamp = timestamp; UserName = userName; Operation = operation }
