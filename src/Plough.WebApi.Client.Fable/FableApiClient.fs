namespace rec Plough.WebApi.Client.Fable

open System
open Browser

type [<AbstractClass; Sealed>] ApiClient =
    static member init (?defaultTimeout : TimeSpan, ?debugLog : bool) =
        let debugLog = defaultArg debugLog false
        
        new Plough.WebApi.Client.ApiClient (
            get  = Core.makeAPIGetPromise debugLog,
            post = Core.makeAPIPostPromise debugLog,
            getBinary = Core.makeAPIGetBinaryPromise debugLog,
            postBinary = Core.makeAPIPostBinaryPromise debugLog,
            defaultTimeout = defaultTimeout,
            dispose = ignore
        )

[<RequireQualifiedAccess>]
module internal Core =
    open Fable.Core
    open Fable.Core.JsInterop
    open Fetch.Types
    open Fable.Core.JS
    
    let private logServerError (debugLog : bool) (relativeUrl : string) (statusCode : int) (response : string) =
        if debugLog then
            printfn $"Call to %s{relativeUrl} did not succeed. Status code: %i{statusCode}. Response: \n %s{response}"
    
    let private logClientError (debugLog : bool) (relativeUrl : string) (reason : string) =
        if debugLog then
            printfn $"Call to %s{relativeUrl} did not succeed. Reason: %s{reason}"
    
    let private sendWithTimeoutAsync props (relativeUrl : string) (timeout : TimeSpan) =
        async {
            // By default a fetch() request timeouts at the time indicated by the browser.
            // In Chrome a network request timeouts at 300 seconds, while in Firefox at 90 seconds.
            // fetch() API by itself doesn't allow canceling programmatically a request.
            // To stop a request at the desired time you need additionally an abort controller.
            // https://dmitripavlutin.com/timeout-fetch-request/
            
            let controller = Fetch.newAbortController()
            // (fun () -> controller.abort()) must be a full not inlined lambda, - otherwise `this` context not passed
            // https://stackoverflow.com/questions/47608666/settimeout-illegal-invocation-typeerror-illegal-invocation
            let timeoutId = setTimeout (fun () -> controller.abort ()) (timeout.TotalMilliseconds |> Math.round |> int)
            
            // Would use Fable.PowerPack.Fetch.fetchAs, but it's not quite flexible enough...
            let propsKV =
                RequestProperties.Signal controller.signal
                :: props
                |> keyValueList CaseRules.LowerFirst
                :?> RequestInit
            
            try
                // Calling GlobalFetch.fetch directly to avoid calling failwith immediately when response is not Ok.
                let! response =
                    GlobalFetch.fetch(RequestInfo.Url relativeUrl, propsKV)
                    |> Async.AwaitPromise    
                clearTimeout timeoutId
                return Ok response
            with
            | _ when controller.signal.aborted ->
                // Fable doesn't propagate name of JS error (err.Name == "AbortError")
                // Therefore we catch exception in case signal aborted
                return Error "Timed out."
        }
    
    // Given a list of RequestProperties and a url, construct a Fable Javascript
    // Promise object that calls the Demetrix API server,
    // and returns either an object of the specified type 'a,
    // or a failure state with an explanatory message.
    //   The server will return the object 'a encapsulated in a Response object,
    // and this function will unwrap it and check for a failure or error reported by
    // the server.
    let private makeAPIPromise props (debugLog : bool) (relativeUrl : string) (timeout : TimeSpan) (readContent : Response -> Promise<'a>) =
        async {
            match! sendWithTimeoutAsync props relativeUrl timeout with
            | Ok response ->
                if not response.Ok then
                    let! data = response.text() |> Async.AwaitPromise
                    logServerError debugLog relativeUrl response.Status data
                    return Error data
                else
                    let! data = readContent response |> Async.AwaitPromise
                    return Ok data
            | Error reason ->
                logClientError debugLog relativeUrl reason
                return Error reason
        }
        #if !FABLE_COMPILER
        |> Async.StartAsTask
        #endif
    
    // Construct the headers for an HTTP GET request to the given URL,
    // then use makeAPIPromise to generate a promise that calls the URL
    // and returns an object of type 'a, or a failure state.
    let makeAPIGetPromise (debugLog : bool) (relativeUrl : string) (timeout : TimeSpan) =
        let props = 
            [ RequestProperties.Credentials RequestCredentials.Include
              RequestProperties.Cache RequestCache.Nostore ]
        makeAPIPromise props debugLog relativeUrl timeout (fun r -> r.text())
    
    
    // Construct the headers and body for an HTTP POST request to the given URL,
    // encoding the given object into JSON as the body of the request,
    // then use makeAPIPromise to generate a promise that calls the URL
    // and returns an object of type 'b, or a failure state.
    let makeAPIPostPromise (debugLog : bool) (jsonToPost : string option) (relativeUrl : string) (timeout : TimeSpan) =
        let body = defaultArg jsonToPost "null"
        if debugLog then console.log $"posting: {body}"
    
        let props =
            [ RequestProperties.Method HttpMethod.POST
              Fetch.requestHeaders [
                HttpRequestHeaders.ContentType "application/json" ]
              RequestProperties.Credentials RequestCredentials.Include 
              RequestProperties.Body !^body ]
        makeAPIPromise props debugLog relativeUrl timeout (fun r -> r.text())
    
    // Construct the headers and body for an HTTP POST request to the given URL,
    // encoding the given object into JSON as the body of the request,
    // then use makeAPIPromise to generate a promise that calls the URL
    // and returns an object of type 'b, or a failure state.
    let makeAPIPostBinaryPromise (debugLog : bool) (contentToPost : byte []) (relativeUrl : string) (timeout : TimeSpan) =
        if debugLog then console.log $"posting: %i{contentToPost.Length} bytes"
    
        let props =
            [ RequestProperties.Method HttpMethod.POST
              Fetch.requestHeaders [ HttpRequestHeaders.ContentType "application/octetStream"]
              RequestProperties.Credentials RequestCredentials.Include 
              RequestProperties.Body !^ (Blob.Create(blobParts = [| contentToPost|])) ]
        makeAPIPromise props debugLog relativeUrl timeout (fun r -> r.text())
        
        
    // GET URL and return raw byte stream
    let makeAPIGetBinaryPromise (debugLog : bool) (relativeUrl : string) (timeout : TimeSpan) =
        if debugLog then console.log $"getting: bytes from %s{relativeUrl}"
        let props =
            [ RequestProperties.Method HttpMethod.GET
              // Fetch.requestHeaders [ HttpRequestHeaders.ContentType "application/octetStream"]
              RequestProperties.Credentials RequestCredentials.Include ]
            
        makeAPIPromise props debugLog relativeUrl timeout (fun response -> promise {
            let! blob = response.blob()
            let nativeUint8Array = Constructors.Uint8Array.Create(blob)
            let asByteArray = unbox<byte[]> nativeUint8Array
            return asByteArray
        })
        