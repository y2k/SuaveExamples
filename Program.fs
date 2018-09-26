open System
open System.Text
open Suave
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Cookie
open Suave.Swagger.Swagger
open Suave.Swagger.FunnyDsl
open Suave.Swagger

module Simple =
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
    let start () =
        choose [
            GET >=> path "/" >=> OK "It works"
            GET >=> pathScan "/login/%s" login >=> OK "OK"
            GET >=> path "/data" >=> getData
        ] 
        |> startWebServer defaultConfig

module Swagger' =
    type TimeResult = TimeResult
    let api = 
        swagger {
            for route in getting <| simpleUrl "/data" |> thenReturns Simple.getData do
                yield route |> addResponse 200 "Return data" (Some typeof<string>)
                yield route |> supportsJsonAndXml
            for route in getOf <| pathScan "/login/%s" Simple.login do
                yield urlTemplate Of route is "/login/{email}"
                yield parameter "email" Of route
                    (fun p -> { p with Type = (Some typeof<string>); In = Path })
                yield route |> addResponse 200 "Login via email" None
                yield route |> supportsJsonAndXml
        }
    let start () =
        startWebServer defaultConfig api.App

[<EntryPoint>]
let main _ = 
    Swagger'.start ()
    0
