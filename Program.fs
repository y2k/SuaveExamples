open System
open System.Text
open Suave
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Cookie

let private cookieState =
    CookiesState.create
        (Encoding.ASCII.GetBytes "01234567890123456789012345678901")
        "userId"
        "userId"
        (MaxAge <| TimeSpan.FromDays 1.)
        false

type Snapshot = { id : int }
type SnapshotsResult = { items : Snapshot list }

let login (email : string) =
    Cookie.updateCookies
        cookieState
        (fun _ -> Encoding.UTF8.GetBytes email)

let getData =
    Cookie.cookieState
        cookieState
        (fun _ -> Choice2Of2 <| RequestErrors.FORBIDDEN "")
        (fun _ -> Choice2Of2 <| RequestErrors.FORBIDDEN "")
        (context (fun ctx ->
                    let x = ctx.userState.["userId"] :?> byte[] |> Encoding.UTF8.GetString
                    OK <| sprintf "User-ID = %O" x))

[<EntryPoint>]
let main _ = 
    let app =
        choose [
            GET >=> path "/" >=> OK "It works"
            GET >=> pathScan "/login/%s" login >=> OK "OK"
            GET >=> path "/data" >=> getData
        ]
    startWebServer defaultConfig app
    0
