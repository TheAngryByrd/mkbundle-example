namespace App 
open System
open System.Threading
open Owin
open Microsoft.Owin.Hosting
module NancyOnMono =

    open Nancy
    type HelloWorldModule() as this =
        inherit NancyModule()
        do
            this.Get.["/"] <- fun _ -> "Hello From Nancy on Mono!" :> obj 


module Main = 

    type Startup () =
        member this.Configuration(app :IAppBuilder) =
            app.UseNancy() |> ignore
            ()

    let log msg =
        printfn "%s - %s" (DateTimeOffset.UtcNow.ToString("o")) msg
    [<EntryPoint>]
    let main argv =
        log <| sprintf "Starting app"
        let url = "http://127.0.0.1:8083";

        use app = WebApp.Start<Startup>(url)
        let quitEvent = new ManualResetEvent(false)
        Console.CancelKeyPress.Add(fun (args) -> 
            quitEvent.Set() |> ignore
            args.Cancel <- true)
        log <| sprintf "Listening on %s" url
        quitEvent.WaitOne() |> ignore
        0 // return an integer exit code
