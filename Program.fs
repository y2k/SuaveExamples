open Suave
open Suave.Cookie
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Swagger.FunnyDsl
open Suave.Swagger.Swagger
open System
open System.Text

module Simple =

    let (^) f a = f a

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
                        let x = ctx.userState.["userId"] :?> byte [] |> Encoding.UTF8.GetString
                        OK <| sprintf "User-ID = %O" x))
    let loginViaGoogle =
        request (fun r ->
            r.formData "token"
            |> Option.ofChoice
            |> Option.map (fun x -> login x >=> OK "")
            |> Option.defaultValue (RequestErrors.FORBIDDEN ""))

    let showEnv =
        request ^ fun r ->
            let env = r.queryParamOpt "name" |> Option.get |> snd |> Option.get
            let envVal = System.Environment.GetEnvironmentVariable env
            sprintf "%s = %s\n" env envVal |> OK

    let time = 
        request (fun _ -> OK(sprintf "Server time = %O\n" DateTime.Now))
    let start() =
        choose [
            GET >=> path "/time" >=> OK(sprintf "Server time = %O" DateTime.Now)
            POST >=> path "/loginViaGoogle" >=> loginViaGoogle
            GET >=> path "/" >=> OK "It works"
            GET >=> pathScan "/login/%s" login >=> OK "OK"
            GET >=> path "/data" >=> getData
        ]
        |> startWebServer defaultConfig

module Swagger' =
    type TimeResult = TimeResult
    let api =
        swagger {
            for route in getting <| simpleUrl "/showEnv" |> thenReturns Simple.showEnv do
                yield parameter "name" Of route
                    (fun p -> { p with Type = (Some typeof<string>) })
            for route in getting <| simpleUrl "/time" |> thenReturns Simple.time do
                yield route |> addResponse 200 "Return data" (Some typeof<string>)
            for route in posting <| simpleUrl "/loginViaGoogle" |> thenReturns Simple.loginViaGoogle do
                yield parameter "token" Of route
                    (fun p -> { p with Type = (Some typeof<string>); In = FormData })
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
    open System.Net
    let start() =
        let config = { defaultConfig with
                           bindings = [ HttpBinding.create HTTP (IPAddress.Parse "0.0.0.0") 8080us ] }
        startWebServer config api.App

[<EntryPoint>]
let main _ =
    Swagger'.start()
    // Simple.start ()
    0
