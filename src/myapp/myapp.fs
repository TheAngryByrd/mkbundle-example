namespace App 
open System
open System.Threading
open Owin
open Microsoft.Owin.Hosting
open NodaTime // conf
open Hopac // conf
open Logary // normal usage
open Logary.Message // normal usage
open Logary.Configuration // conf
open Logary.Targets // conf
open Logary.Metric // conf
open Logary.Metrics // conf
open System.Threading // control flow
module NancyOnMono =

    open Nancy
    type HelloWorldModule() as this =
        inherit NancyModule()
        do
            this.Get.["/"] <- fun _ -> "Hello From Nancy on Mono!" :> obj 


module Main = 

    let randomMetric (pn : PointName) : Job<Metric> =
        let reducer state = function
        | _ -> state

        let ticker pn (rnd : Random, prevValue) =
            let value = rnd.NextDouble()
            let msg = Message.gauge pn (Float value)
            (rnd, value), [| msg |]

        let state = Random(), 0.0

        Metric.create reducer state (ticker pn)

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
        use logary =
            // main factory-style API, returns IDisposable object
            withLogaryManager "Logary.Examples.ConsoleApp" (
            // output to the console
                withTargets [
                    LiterateConsole.create (LiterateConsole.empty) "console"
                ] >>
                // continuously log CPU stats
                withMetrics [
                    MetricConf.create (Duration.FromMilliseconds 500L) "random" randomMetric
                ] >>
                // "link" or "enable" the loggers to send everything to the configured target
                withRules [
                    Rule.createForTarget "console"
                ]
            )
            // "compile" the Logary instance
            |> run

        // Get a new logger. Also see Logging.getLoggerByName for statically getting it
        let logger =
            logary.getLogger (PointName [| "Logary"; "Samples"; "main" |])

        // log something
        logger.info (
            eventX "User with {userName} loggedIn"
            >> setField "userName" "haf")
        quitEvent.WaitOne() |> ignore
        0 // return an integer exit code
