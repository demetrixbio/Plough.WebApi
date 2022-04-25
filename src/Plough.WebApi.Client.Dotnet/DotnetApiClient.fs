namespace rec Plough.WebApi.Client.Dotnet

open System
open System.Threading
open System.Threading.Tasks
open Plough.ControlFlow
open System.Net.Http
open Plough.WebApi.Client.Dotnet

type SessionCookie =
    { Name : string
      Value : string }

type Auth =
    | AccessToken of (unit -> Either<string>)
    | SessionCookie of (unit -> SessionCookie)
    | NoAuth

type [<AbstractClass; Sealed>] ApiClient =
    /// <summary>Builds a new dotnet api client with specified auth, base url and default timeout.</summary>
    /// <param name="httpClient">Http client used for handling requests.
    /// Given client must not issue any calls before it's passed to init method, otherwise exception will be thrown upon setting default timeout.</param>
    /// <param name="auth">Auth type - OAuth access token retrieval function / cached session cookie / none.</param>
    /// <param name="baseUrl">The base url of api server.</param>
    /// <param name="defaultTimeout">Default timeout for api calls. If none provided, `Plough.WebApi.Client.Core.defaultRequestTimeout` is used.</param>
    /// <param name="debugLog">Log debug information upon making api requests.</param>
    /// <returns>Configured api client.</returns>
    static member private init (httpClient : HttpClient, auth : Auth, baseUrl : Uri, defaultTimeout : TimeSpan option, debugLog : bool option) =
        // https://makolyte.com/csharp-how-to-change-the-httpclient-timeout-per-request/
        // HttpClient with infinite timeout is used in combination with CancellationTokenSource per request
        // therefore we can control timeout on per request basis.
        // Default timeout for both Dotnet and Fable is specified in Plough.WebApi.Core.ApiClient
        httpClient.Timeout <- Timeout.InfiniteTimeSpan
        
        let debugLog = defaultArg debugLog false
        
        new Plough.WebApi.Client.ApiClient (
            get  = Core.send auth httpClient baseUrl debugLog HttpMethod.Get None,
            post = Core.send auth httpClient baseUrl debugLog HttpMethod.Post,
            getBinary = Core.getBinaryImplementation auth httpClient baseUrl debugLog,
            postBinary = Core.sendBinary auth httpClient baseUrl debugLog,
            defaultTimeout = defaultTimeout,
            dispose = httpClient.Dispose
        )
    
    
    /// <summary>Builds a new dotnet api client with specified auth, base url and default timeout.</summary>
    /// <param name="auth">Auth type - OAuth access token retrieval function / cached session cookie / none.</param>
    /// <param name="baseUrl">The base url of api server.</param>
    /// <param name="httpClient">Http client used for handling requests.
    /// Given client must not issue any calls before it's passed to init method, otherwise exception will be thrown upon setting default timeout.</param>
    /// <param name="defaultTimeout">Default timeout for api calls. If none provided, `Plough.WebApi.Client.Core.defaultRequestTimeout` is used.</param>
    /// <param name="debugLog">Log debug information upon making api requests.</param>
    /// <returns>Configured api client.</returns>
    static member init (auth : Auth, baseUrl : Uri, httpClient : HttpClient, ?defaultTimeout : TimeSpan, ?debugLog : bool) =
        ApiClient.init(httpClient, auth, baseUrl, defaultTimeout, debugLog)
    
    /// <summary>Builds a new dotnet api client with specified auth, base url and default timeout.</summary>
    /// <param name="auth">Auth type - OAuth access token retrieval function / cached session cookie / none.</param>
    /// <param name="baseUrl">The base url of api server.</param>
    /// <param name="defaultTimeout">Default timeout for api calls. If none provided, `Plough.WebApi.Client.Core.defaultRequestTimeout` is used.</param>
    /// <param name="ignoreSslPolicyErrors">Ignore ssl policy errors - useful in case of local testing without properly configured server certificate.</param>
    /// <param name="debugLog">Log debug information upon making api requests.</param>
    /// <returns>Configured api client.</returns>
    static member init (auth : Auth, baseUrl : Uri, ?defaultTimeout : TimeSpan, ?ignoreSslPolicyErrors : bool, ?debugLog : bool) =
        // http client must not store any cookies from a response
        let handler = new HttpClientHandler(UseCookies = false)
        // in case of issues with certificate on local env
        if Option.defaultValue false ignoreSslPolicyErrors then
            handler.ServerCertificateCustomValidationCallback <-
                (fun sender certificate chain sslPolicyErrors -> true)
        // https://makolyte.com/csharp-how-to-change-the-httpclient-timeout-per-request/
        // HttpClient with infinite timeout is used in combination with CancellationTokenSource per request
        // therefore we can control timeout on per request basis.
        // Default timeout for both Dotnet and Fable is specified in Plough.WebApi.Core.ApiClient
        let httpClient = new HttpClient(handler)
        ApiClient.init(httpClient, auth, baseUrl, defaultTimeout, debugLog)
        

[<RequireQualifiedAccess>]
module internal Core =
    open System.Net.Http.Headers
    open System.Text
    
    let private injectAuthHeaders (auth : Auth) (requestMessage : HttpRequestMessage) =
        match auth with
        | AccessToken retriever ->
            let accessToken =
                match retriever () with
                | Success token | SuccessWithWarning (token, _) -> token
                | Failure error -> error |> FailureMessage.unwrap |> failwith
            requestMessage.Headers.Authorization <- AuthenticationHeaderValue("Bearer", accessToken)
        | SessionCookie retriever ->
            let cookie = retriever()
            requestMessage.Headers.TryAddWithoutValidation("Cookie", $"%s{cookie.Name}=%s{cookie.Value}") |> ignore
        | NoAuth -> ()
    
    let private logServerError (debugLog : bool) (relativeUrl : string) (statusCode : Net.HttpStatusCode) (response : string) =
        if debugLog then
            printfn $"Call to %s{relativeUrl} did not succeed. Status code: %A{statusCode}. Response: \n %s{response}"
    
    let private logClientError (debugLog : bool) (relativeUrl : string) (reason : string) =
        if debugLog then
            printfn $"Call to %s{relativeUrl} did not succeed. Reason: %s{reason}"
    
    let private sendWithTimeoutAsync (client : HttpClient) (message : HttpRequestMessage) (timeout : TimeSpan) =
        task {
            use cts = new CancellationTokenSource(timeout)
            try
                let! response = client.SendAsync(message, cts.Token)
                return Ok response
            with
            | :? TaskCanceledException ->
                if cts.IsCancellationRequested then
                    return Error "User cancelled."
                else
                    return Error "Timed out."
        }
        
    let private sendAsync (auth : Auth) (client : HttpClient) (message : HttpRequestMessage) (timeout : TimeSpan)
                          (debugLog : bool) (relativeUrl : string) (readContent : HttpResponseMessage -> Task<'response>)  =
        task {
            injectAuthHeaders auth message
            match! sendWithTimeoutAsync client message timeout with
            | Ok response ->
                if response.IsSuccessStatusCode then
                    let! content = readContent response
                    return Ok content
                else
                    let! content = response.Content.ReadAsStringAsync()
                    logServerError debugLog relativeUrl response.StatusCode content
                    return Error content
            | Error error -> 
                logClientError debugLog relativeUrl error
                return Error error
        }
    
    let send auth (client : HttpClient) (baseUrl : Uri) (debugLog : bool) =
        fun httpMethod (payload : string option) (relativeUrl : string) (timeout : TimeSpan) ->
        task {
            use message = new HttpRequestMessage(httpMethod, Uri(baseUrl, relativeUrl))
            if payload.IsSome then
                message.Content <- new StringContent(payload.Value, Encoding.UTF8, "application/json")
                
            let! response =
                sendAsync auth client message timeout debugLog relativeUrl (fun response ->
                    response.Content.ReadAsStringAsync())
            return response
        }
    
    let sendBinary auth (client : HttpClient) (baseUrl : Uri) (debugLog : bool) =
        fun (payload : byte []) (relativeUrl : string) (timeout : TimeSpan) ->
        task {
            use message = new HttpRequestMessage(HttpMethod.Post, Uri(baseUrl, relativeUrl))
            message.Content <- new ByteArrayContent(payload)
            let! response =
                sendAsync auth client message timeout debugLog relativeUrl (fun response ->
                    response.Content.ReadAsStringAsync())
            return response
        }
    
    let getBinaryImplementation auth (client : HttpClient) (baseUrl : Uri) (debugLog : bool) =
        fun (relativeUrl : string) (timeout : TimeSpan) ->
        task {
            use message = new HttpRequestMessage(HttpMethod.Get, Uri(baseUrl, relativeUrl))
            let! response =
                sendAsync auth client message timeout debugLog relativeUrl (fun response ->
                    response.Content.ReadAsByteArrayAsync())
            return response
        }