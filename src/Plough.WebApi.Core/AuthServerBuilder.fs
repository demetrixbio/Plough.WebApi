namespace Plough.WebApi.Server.Builder.Auth

open Plough.WebApi.Server

/// <summary>
/// Server builder, that contains all functions required to build Giraffe/Suave web server endpoints, used to indirect dependencies of underlying web server from core library
/// </summary>
type ServerBuilder<'ctx> =
    
    // ---------------------------
    // Auth handling functions
    // ---------------------------
    abstract member isOffline : bool
    abstract member isLoggedIn : HttpHandler<'ctx>
    abstract member authenticate : HttpHandler<'ctx>
    abstract member authenticateJSON : HttpHandler<'ctx>
    abstract member login : HttpHandler<'ctx>
    abstract member logout : HttpHandler<'ctx>
    abstract member requirePolicy : policy : string -> HttpHandler<'ctx>
    abstract member identityClaims : HttpHandler<'ctx>
    abstract member identity : next : HttpFunc<'ctx> -> ctx : 'ctx -> HttpFuncResult<'ctx>