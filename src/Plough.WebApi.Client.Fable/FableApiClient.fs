namespace rec Plough.WebApi.Client.Fable

open Browser

type ApiClient() =
    inherit Plough.WebApi.Client.ApiClient (
        get  = Core.makeAPIGetPromise,
        post = Core.makeAPIPostPromise,
        getBinary = Core.makeAPIGetBinaryPromise,
        postBinary = Core.makeAPIPostBinaryPromise
    )

[<RequireQualifiedAccess>]
module internal Core =
    open Fable.Core
    open Fable.Core.JsInterop
    open Fetch.Types
    open Fable.Core.JS
    
    // Given a list of RequestProperties and a url, construct a Fable Javascript
    // Promise object that calls the Demetrix API server,
    // and returns either an object of the specified type 'a,
    // or a failure state with an explanatory message.
    //   The server will return the object 'a encapsulated in a Response object,
    // and this function will unwrap it and check for a failure or error reported by
    // the server.
    let makeAPIPromise props relativeUrl =
        async {
            // Would use Fable.PowerPack.Fetch.fetchAs, but it's not quite flexible enough...
            let propsKV = keyValueList CaseRules.LowerFirst props :?> RequestInit
            // Calling GlobalFetch.fetch directly to avoid calling failwith immediately when response is not Ok.
            let! response = GlobalFetch.fetch(RequestInfo.Url relativeUrl, propsKV) |> Async.AwaitPromise
            if not response.Ok then
                let! data = response.text() |> Async.AwaitPromise
                return Error data
            else
                let! data = response.text() |> Async.AwaitPromise
                return Ok data
        }
        #if !FABLE_COMPILER
        |> Async.StartAsTask
        #endif
    
    // Construct the headers for an HTTP GET request to the given URL,
    // then use makeAPIPromise to generate a promise that calls the URL
    // and returns an object of type 'a, or a failure state.
    let makeAPIGetPromise (relativeUrl : string) =
        let props = 
            [ RequestProperties.Credentials RequestCredentials.Include
              RequestProperties.Cache RequestCache.Nostore ]
        makeAPIPromise props relativeUrl
    
    
    // Construct the headers and body for an HTTP POST request to the given URL,
    // encoding the given object into JSON as the body of the request,
    // then use makeAPIPromise to generate a promise that calls the URL
    // and returns an object of type 'b, or a failure state.
    let makeAPIPostPromise (jsonToPost : string option) (relativeUrl : string) =
        let body = defaultArg jsonToPost "null"
        console.log("posting: " + body)
    
        let props =
            [ RequestProperties.Method HttpMethod.POST
              Fetch.requestHeaders [
                HttpRequestHeaders.ContentType "application/json" ]
              RequestProperties.Credentials RequestCredentials.Include 
              RequestProperties.Body !^body ]
        makeAPIPromise props relativeUrl
    
    // Construct the headers and body for an HTTP POST request to the given URL,
    // encoding the given object into JSON as the body of the request,
    // then use makeAPIPromise to generate a promise that calls the URL
    // and returns an object of type 'b, or a failure state.
    let makeAPIPostBinaryPromise (contentToPost : byte []) (relativeUrl : string) =
        console.log(sprintf "posting: %i bytes" contentToPost.Length)
    
        let props =
            [ RequestProperties.Method HttpMethod.POST
              Fetch.requestHeaders [ HttpRequestHeaders.ContentType "application/octetStream"]
              RequestProperties.Credentials RequestCredentials.Include 
              RequestProperties.Body !^ (Blob.Create(blobParts = [| contentToPost|]))
            ]
        makeAPIPromise props relativeUrl
        
        
    // GET URL and return raw byte stream
    let makeAPIGetBinaryPromise (relativeUrl : string) =
        console.log(sprintf "getting: bytes from %s" relativeUrl)
        async {
            let props =
                [ RequestProperties.Method HttpMethod.GET
                  // Fetch.requestHeaders [ HttpRequestHeaders.ContentType "application/octetStream"]
                  RequestProperties.Credentials RequestCredentials.Include 
                ]
            // Would use Fable.PowerPack.Fetch.fetchAs, but it's not quite flexible enough...
            let propsKV = keyValueList CaseRules.LowerFirst props :?> RequestInit
            // Calling GlobalFetch.fetch directly to avoid calling failwith immediately when response is not Ok.
            let! response = GlobalFetch.fetch(RequestInfo.Url relativeUrl, propsKV) |> Async.AwaitPromise
            
            if not response.Ok then
                let! data = response.text() |> Async.AwaitPromise
                return Error data
            else
                let! blob = response.blob() |> Async.AwaitPromise
                return Constructors.Uint8Array.Create(blob) |> unbox<byte[]> |> Ok
        } 
        #if !FABLE_COMPILER
        |> Async.StartAsTask
        #endif