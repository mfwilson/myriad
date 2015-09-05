namespace Myriad

open System
open System.Threading

/// Lock-free, mutable list that supports multi-threading scenarios.
/// http://fssnip.net/ok
type LockFreeList<'T when 'T : equality>(init) =
    let mutable items: 'T list = init

    member x.Value = items

    member x.Update updater =
        let current = items
        let newItems = updater current
        if not <| obj.ReferenceEquals(current, Interlocked.CompareExchange(&items, newItems, current))
            then x.Update updater
            else x

    member x.Add item = x.Update (fun L -> item::L)
    member x.Remove item = x.Update (fun L -> List.filter (fun i -> i <> item) L)

    static member empty = new LockFreeList<'T>([])
    static member add item (l:LockFreeList<'T>) = l.Add item
    static member get (l:LockFreeList<'T>) = l.Value
    static member remove item (l:LockFreeList<'T>) = l.Remove item

(* Usage Example *)

(*
type ObservableSource<'a>() =
    let subscriptionList = MutableList<IObserver<'a>>.empty        
    interface IObservable<'a> with
        member x.Subscribe observer =
            subscriptionList.Add observer |> ignore
            { new IDisposable with
                member x.Dispose() = subscriptionList.Remove observer |> ignore
            }

    member x.Complete() = subscriptionList.Value |> List.iter (fun obs -> obs.OnCompleted())
    member x.Push data = subscriptionList.Value |> List.iter (fun observer -> observer.OnNext data)
    member x.Error exn = subscriptionList.Value |> List.iter (fun obs -> obs.OnError exn)

    static member create() = new ObservableSource<'a>()
    static member get (source:ObservableSource<'a>) = source :> IObservable<'a>

let source = ObservableSource<int>()
source |> Observable.subscribe (fun i -> printfn "%d" i) |> ignore

source.Push 123   // 123 should be printed.
*)
