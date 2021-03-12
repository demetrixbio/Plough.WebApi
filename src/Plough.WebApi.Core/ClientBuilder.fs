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
    
#if FABLE_COMPILER
    member inline x.Get<'response>(relativeUrl : string, ?arbitraryType: bool) =
#else
    member x.Get<'response>(relativeUrl : string, ?arbitraryType: bool) =
#endif
        let url = Url.combine baseUrl relativeUrl
        arbitraryType
        |> Option.map (fun s -> client.Get<'response>(url, s))
        |> Option.defaultWith (fun () -> client.Get<'response>(url))

#if FABLE_COMPILER
    member inline x.Post(relativeUrl, ?payload : 'request) =
#else
    member x.Post<'request, 'response>(relativeUrl, ?payload : 'request) =
#endif
        let url = Url.combine baseUrl relativeUrl
        payload
        |> Option.map (fun s -> client.Post<'request, 'response>(url, s))
        |> Option.defaultWith (fun () -> client.Post<'request, 'response>(url))
        
#if FABLE_COMPILER
    /// Get relativeURL and return byte array (empty if not found)
    member inline x.GetBinary(relativeUrl) : TaskEither<byte []>  =
#else
    member x.GetBinary<'response>(relativeUrl:string) : TaskEither<byte []>=
#endif
        let url = Url.combine baseUrl relativeUrl
        client.GetBinary<'response>(url)
        
#if FABLE_COMPILER
    /// Send binary to relativeUrl and return JSON response
    member inline x.PostBinary(relativeUrl, payload : byte []):TaskEither<'response> =
#else
    /// Send binary to relativeUrl and return JSON response
    member x.PostBinary<'response>(relativeUrl, payload : byte []) =
#endif
        let url = Url.combine baseUrl relativeUrl
        client.PostBinary<'response>(url, payload)
