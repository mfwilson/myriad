namespace Myriad

open System

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
