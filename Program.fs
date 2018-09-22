open System
open Suave
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Cookie


type Snapshot = { id : int }
type SnapshotsResult = { items : Snapshot list }

let foo =
    { items = [ { id = 1 }; { id = 2 }; { id = 3 } ] }
    |> string
    |> OK

let foo2 =
    Authentication.authenticated (MaxAge <| TimeSpan.FromDays 1.) true

let foo3 =
    Authentication.authenticateWithLogin
        (MaxAge <| TimeSpan.FromDays 1.)
        "/login"
        foo

[<EntryPoint>]
let main _ = 
    startWebServer defaultConfig (GET >=> path "/1" >=> foo2 >=> foo)
    startWebServer defaultConfig (GET >=> path "/2" >=> foo3)
    0
