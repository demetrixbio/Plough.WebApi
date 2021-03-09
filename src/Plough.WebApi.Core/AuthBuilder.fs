namespace Plough.WebApi.Server.Auth

open Plough.WebApi.Server

/// <summary>
/// Server builder, that contains all functions required to build Giraffe/Suave web server endpoints, used to indirect dependencies of underlying web server from core library
/// </summary>
type ServerBuilder<'context> =
    
    // ---------------------------
    // Auth handling functions
    // ---------------------------
    abstract member isOffline : bool
    abstract member isLoggedIn : HttpHandler<'context>
    abstract member authenticate : HttpHandler<'context>
    abstract member authenticateJSON : HttpHandler<'context>
    abstract member login : HttpHandler<'context>
    abstract member logout : HttpHandler<'context>
    abstract member requirePolicy : policy : string -> HttpHandler<'context>
    abstract member identityClaims : HttpHandler<'context>
    abstract member identity : next : HttpFunc<'context> -> ctx : 'context -> HttpFuncResult<'context>
    
[<AutoOpen>]
module Builder =
    
    let inline isOffline (builder : #ServerBuilder<'context>) : bool =
        builder.isOffline
    
    let inline authenticate (builder : #ServerBuilder<'context>) : HttpHandler<'context> =
        builder.authenticate
    
    let inline isLoggedIn (builder : #ServerBuilder<'context>) : HttpHandler<'context> =
        builder.isLoggedIn
        
    let inline authenticateJSON (builder : #ServerBuilder<'context>) : HttpHandler<'context> =
        builder.authenticateJSON
        
    let inline login (builder : #ServerBuilder<'context>) : HttpHandler<'context> =
        builder.login
        
    let inline logout (builder : #ServerBuilder<'context>) : HttpHandler<'context> =
        builder.logout
        
    let inline requirePolicy (policy : string) (builder : #ServerBuilder<'context>) : HttpHandler<'context> =
        builder.requirePolicy policy
        
    let inline identityClaims (builder : #ServerBuilder<'context>) : HttpHandler<'context> =
        builder.identityClaims
        
    let inline identity (builder : #ServerBuilder<'context>) : HttpHandler<'context> =
        builder.identity