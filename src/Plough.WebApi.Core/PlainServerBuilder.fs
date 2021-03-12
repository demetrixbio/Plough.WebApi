namespace Plough.WebApi.Server.Builder.Plain

open Plough.ControlFlow
open Plough.WebApi.Server

/// <summary>
/// Server builder, that contains all functions required to build Giraffe/Suave web server endpoints, used to indirect dependencies of underlying web server from core library
/// </summary>
type ServerBuilder<'ctx> =
    
    // ---------------------------
    // Plain (no dependency injection) route handling functions
    // ---------------------------

    abstract member makeDownloadHandlerWithArg<'a> : download : DownloadWithObject<'a> -> param : 'a -> HttpHandler<'ctx>
    abstract member makeDownloadHandlerWithArgAsync<'a> : download : DownloadWithObjectAsync<'a> -> param : 'a -> HttpHandler<'ctx>
    abstract member makeDownloadHandlerNoArg : download : DownloadWithObject<unit> -> HttpHandler<'ctx>
    abstract member makeDownloadHandlerNoArgAsync : download : DownloadWithObjectAsync<unit> -> HttpHandler<'ctx>
    abstract member makeDownloadHandlerWithObj<'a> : download : DownloadWithObject<'a> -> HttpHandler<'ctx>
    abstract member makeDownloadHandlerWithObjAsync<'a> : download : DownloadWithObjectAsync<'a> -> HttpHandler<'ctx>
    
    abstract member makeJSONHandler<'b> : call : Call<'b> -> HttpHandler<'ctx>
    abstract member makeJSONHandlerAsync<'b> : call : CallAsync<'b> -> HttpHandler<'ctx>
    
    abstract member makeJSONHandlerWithArg<'a, 'b> : call : CallWithObject<'a, 'b> -> i : 'a -> HttpHandler<'ctx>
    abstract member makeJSONHandlerWithArgAsync<'a, 'b> : call : CallWithObjectAsync<'a, 'b> -> i : 'a -> HttpHandler<'ctx>
    
    abstract member makeJSONHandlerWithTwoArg<'a, 'b, 'c> : call : CallWithTwoObjects<'a, 'b, 'c> -> i : 'a -> HttpHandler<'ctx>
    abstract member makeJSONHandlerWithTwoArgAsync<'a, 'b, 'c> : call : CallWithTwoObjectsAsync<'a, 'b, 'c> -> i : 'a -> HttpHandler<'ctx>
    
    abstract member makeJSONHandlerWithQueryParam<'a, 'b> : call : CallWithObject<'a, 'b> -> HttpHandler<'ctx>
    abstract member makeJSONHandlerWithQueryParamAsync<'a, 'b> : call : CallWithObjectAsync<'a, 'b> -> HttpHandler<'ctx>
    
    abstract member makeJSONHandlerWithArgQueryParam<'a, 'b, 'c> : call : CallWithTwoObjects<'a, 'b, 'c> -> i : 'a -> HttpHandler<'ctx>
    abstract member makeJSONHandlerWithArgQueryParamAsync<'a, 'b, 'c> : call : CallWithTwoObjectsAsync<'a, 'b, 'c> -> i : 'a -> HttpHandler<'ctx>
    
    abstract member makeJSONHandlerWithObj<'a, 'b> : call : CallWithObject<'a, 'b> -> HttpHandler<'ctx>
    abstract member makeJSONHandlerWithObjAsync<'a, 'b> : call : CallWithObjectAsync<'a, 'b> -> HttpHandler<'ctx>
    
    abstract member makeJSONHandlerWithObjInt<'a, 'b> : call : CallWithIntAndObject<'a, 'b> -> i : int -> HttpHandler<'ctx>
    abstract member makeJSONHandlerWithObjIntAsync<'a, 'b> : call : CallWithIntAndObjectAsync<'a, 'b> -> i : int -> HttpHandler<'ctx>
    
    abstract member makeBinaryPostHandlerWithArgAsync<'a, 'b> : call : ('a -> byte [] -> TaskEither<'b>) -> arg : 'a -> HttpHandler<'ctx>
    abstract member makeBinaryResultHandlerWithArgAsync<'a, 'b> : call : ('a -> Task<byte []>) -> arg : 'a -> HttpHandler<'ctx>