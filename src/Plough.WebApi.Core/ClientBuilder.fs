namespace rec Plough.WebApi.Client

open System

module Url =
    let combine (baseUrl : string) (relativeUrl : string) =
        if String.IsNullOrEmpty(baseUrl) then raise (ArgumentNullException(nameof baseUrl))
        if String.IsNullOrEmpty(relativeUrl) then raise (ArgumentNullException(nameof relativeUrl))
        sprintf "%s/%s" (baseUrl.TrimEnd('/')) (relativeUrl.TrimStart('/'))

//
type ClientBuilderInit =
    | Root of baseUrl : string * client : ApiClient
    | Nested of baseUrl : string * builder : ClientBuilder

[<AbstractClass>]
type ClientBuilder(init : ClientBuilderInit) =
    member x.BaseUrl =
        match init with
        | Root (baseUrl, _) -> baseUrl
        | Nested (baseUrl, builder) -> Url.combine builder.BaseUrl baseUrl
        
    member x.Client =
        match init with
        | Root (_, client) -> client
        | Nested (_, builder) -> builder.Client
    
    member
        #if FABLE_COMPILER
        inline
        #endif
        x.Get<'response>(relativeUrl : string, ?arbitraryType: bool, ?timeout : TimeSpan) =
            let url = Url.combine x.BaseUrl relativeUrl
            x.Client.Get<'response>(url, arbitraryType, timeout)

    member
        #if FABLE_COMPILER
        inline
        #endif
        x.Post<'request, 'response>(relativeUrl, ?payload : 'request, ?timeout : TimeSpan) =
            let url = Url.combine x.BaseUrl relativeUrl
            x.Client.Post<'request, 'response>(url, payload, timeout)
        

    /// Get relativeURL and return byte array (empty if not found)
    member
        #if FABLE_COMPILER
        inline
        #endif
        x.GetBinary(relativeUrl:string, ?timeout : TimeSpan) =
            let url = Url.combine x.BaseUrl relativeUrl
            x.Client.GetBinary(url, timeout)
        
    /// Send binary to relativeUrl and return JSON response
    member
        #if FABLE_COMPILER
        inline
        #endif
        x.PostBinary<'response>(relativeUrl, payload : byte [], ?timeout : TimeSpan) =
            let url = Url.combine x.BaseUrl relativeUrl
            x.Client.PostBinary<'response>(url, payload, timeout)
