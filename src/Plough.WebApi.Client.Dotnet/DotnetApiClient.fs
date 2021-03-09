namespace rec Plough.WebApi.Client.Dotnet

open System
open Plough.ControlFlow
open System.Net.Http

type SessionCookie =
    { Name : string
      Value : string }

type Auth =
    | AccessToken of (unit -> Either<string>)
    | SessionCookie of (unit -> SessionCookie)

type ApiClient (auth : Auth, client, baseUrl) =
    inherit Plough.WebApi.ApiClient (
        get  = Core.send auth client baseUrl HttpMethod.Get None,
        post = Core.send auth client baseUrl HttpMethod.Post,
        getBinary = Core.getBinaryImplementation auth client baseUrl,
        postBinary = Core.sendBinary auth client baseUrl    
    )
    
    new(renewableAccessToken, baseUrl) = ApiClient(renewableAccessToken, Core.proprietaryClient, baseUrl)

[<RequireQualifiedAccess>]
module internal Core =
    open System.Net.Http.Headers
    open System.Text
    
    let injectAuthHeaders (auth : Auth) (requestMessage : HttpRequestMessage) =
        match auth with
        | AccessToken retriever ->
            let accessToken =
                match retriever () with
                | Success token | SuccessWithWarning (token, _) -> token
                | Failure error -> error |> FailureMessage.unwrap |> failwith
            requestMessage.Headers.Authorization <- AuthenticationHeaderValue("Bearer", accessToken)
        | SessionCookie retriever ->
            let cookie = retriever()
            requestMessage.Headers.TryAddWithoutValidation("Cookie", sprintf ".%s=%s" cookie.Name cookie.Value) |> ignore
    
    let send auth (client : HttpClient) baseUrl =
        fun httpMethod (payload : string option) (relativeUrl : string) ->
        async {
            use requestMessage = new HttpRequestMessage(httpMethod, Uri(baseUrl, relativeUrl))
            injectAuthHeaders auth requestMessage
        
            if payload.IsSome then
                requestMessage.Content <- new StringContent(payload.Value, Encoding.UTF8, "application/json")
            
            let! response = client.SendAsync(requestMessage) |> Async.AwaitTask
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            if response.IsSuccessStatusCode then
                return Ok content
            else
                // TODO: this should probably be a parameter to client to set debugging level
                // printfn "Call to %s did not succeed. Status code: %A. Response: \n %s" relativeUrl response.StatusCode content
                return Error content
        } |> Async.StartAsTask
    let sendBinary auth (client : HttpClient) baseUrl =
        fun (payload : byte []) (relativeUrl : string) ->
        async {
            use requestMessage = new HttpRequestMessage(HttpMethod.Post, Uri(baseUrl, relativeUrl))
            injectAuthHeaders auth requestMessage
            requestMessage.Content <- new ByteArrayContent(payload)
            
            let! response = client.SendAsync(requestMessage) |> Async.AwaitTask
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            if response.IsSuccessStatusCode then
                return Ok content
            else
                // TODO: this should probably be a parameter to client to set debugging level
                // printfn "Call to %s did not succeed. Status code: %A. Response: \n %s" relativeUrl response.StatusCode content
                return Error content
        } |> Async.StartAsTask
    
    let getBinaryImplementation auth (client : HttpClient) (baseUrl:Uri) =
        fun (relativeUrl : string) ->
            async {
                use requestMessage = new HttpRequestMessage(HttpMethod.Get, Uri(baseUrl, relativeUrl))
                injectAuthHeaders auth requestMessage
                let! response = client.SendAsync(requestMessage) |> Async.AwaitTask

                if response.IsSuccessStatusCode then
                    let! content = response.Content.ReadAsByteArrayAsync() |> Async.AwaitTask
                    return Ok content
                else
                    let! errorMsg = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                    return Error errorMsg
            } |> Async.StartAsTask
            
    // http client must not store any cookies from a response
    let handler = new HttpClientHandler(UseCookies = false)
    // uncomment in case you have issues with certificate on local env
    //handler.ServerCertificateCustomValidationCallback <- fun sender certificate chain sslPolicyErrors -> true
    let proprietaryClient = new HttpClient(handler)