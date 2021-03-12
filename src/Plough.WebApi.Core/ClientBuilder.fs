namespace Plough.WebApi.Client

open System
open Plough.ControlFlow

module Url =
    let combine (baseUrl : string) (relativeUrl : string) =
        if String.IsNullOrEmpty(baseUrl) then raise (ArgumentNullException(nameof baseUrl))
        if String.IsNullOrEmpty(relativeUrl) then raise (ArgumentNullException(nameof relativeUrl))
        sprintf "%s/%s" (baseUrl.TrimEnd('/')) (relativeUrl.TrimStart('/'))
        
[<AbstractClass>]
type ClientBuilder(baseUrl: string, client : ApiClient) =
    member x.BaseUrl = baseUrl
    member x.Client = client
    
    new (baseUrl: string, builder : ClientBuilder) =
        ClientBuilder(Url.combine builder.BaseUrl baseUrl, builder.Client)
        
    new (builder : ClientBuilder) =
        ClientBuilder(builder.BaseUrl, builder.Client)
    
    member
        #if FABLE_COMPILER
        inline
        #endif
        x.Get<'response>(relativeUrl : string, ?arbitraryType: bool) =
            let url = Url.combine baseUrl relativeUrl
            arbitraryType
            |> Option.map (fun s -> client.Get<'response>(url, s))
            |> Option.defaultWith (fun () -> client.Get<'response>(url))

    member
        #if FABLE_COMPILER
        inline
        #endif
        x.Post<'request, 'response>(relativeUrl, ?payload : 'request) =
            let url = Url.combine baseUrl relativeUrl
            payload
            |> Option.map (fun s -> client.Post<'request, 'response>(url, s))
            |> Option.defaultWith (fun () -> client.Post<'request, 'response>(url))
        

    /// Get relativeURL and return byte array (empty if not found)
    member
        #if FABLE_COMPILER
        inline
        #endif
        x.GetBinary(relativeUrl:string) : TaskEither<byte []>  =
            let url = Url.combine baseUrl relativeUrl
            client.GetBinary(url)
        
    /// Send binary to relativeUrl and return JSON response
    member
        #if FABLE_COMPILER
        inline
        #endif
        x.PostBinary<'response>(relativeUrl, payload : byte []) =
            let url = Url.combine baseUrl relativeUrl
            client.PostBinary<'response>(url, payload)
