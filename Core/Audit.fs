namespace Myriad

open System

type Epoch =
    static member Value = DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)
    static member Ticks = Epoch.Value.Ticks
    
    /// Get the offset from the epoch in microseconds
    static member GetOffset(utcTicks) = (utcTicks - Epoch.Ticks) / 10L

type IAudit =
    abstract Timestamp : Int64 with get
    abstract UserName : String with get

type Audit =
    { Timestamp : Int64; UserName : String }
    
    interface IAudit with
        member x.Timestamp with get() = x.Timestamp
        member x.UserName with get() = x.UserName

    static member Create(timestamp : Int64, userName : String) =
        { Timestamp = timestamp; UserName = userName }

    static member CurrentUser with get() = Environment.UserName

type Audit<'T> =
    { Value : 'T 
      Timestamp : Int64
      UserName : String
    }
    
    interface IAudit with
        member x.Timestamp with get() = x.Timestamp
        member x.UserName with get() = x.UserName

    static member Create(value : 'T, timestamp : Int64, userName : String) =
        { Value = value; Timestamp = timestamp; UserName = userName }
