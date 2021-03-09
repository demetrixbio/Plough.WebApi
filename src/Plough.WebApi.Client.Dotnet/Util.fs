module Plough.WebApi.Client.Dotnet.Util

open Plough.ControlFlow
open System
open System.Collections.Concurrent
open System.Threading

let withExponentialRetry maxRetries delayMs maxDelayMs fn =
    let rec retry pow retries =
        match fn() with
        | Ok { Success.Data = value } -> Either.succeed value
        | Error e when retries > maxRetries -> Either.fail e
        | Error _ ->
            let currentTry = retries + 1
            let currentPow = if (currentTry < 31) then Math.Pow(2., float currentTry - 1.) else pow
            let delay = Math.Min(delayMs * int (pow - 1.) / 2, maxDelayMs)
            printfn "Retrying in %i milliseconds" delay
            Thread.Sleep(delay)
            retry currentPow currentTry
    retry 1. 0

let withCache key f =
    let cache = ConcurrentDictionary<string, ('a * DateTime)>()
    (fun () ->
        either {
            match cache.TryGetValue key with
            | true, (value, expiration) when expiration.AddMinutes(-1.) > DateTime.UtcNow -> return value
            | _ -> 
                let! value = f ()
                return cache.AddOrUpdate(key, value, fun _key _value -> value) |> fst
        })