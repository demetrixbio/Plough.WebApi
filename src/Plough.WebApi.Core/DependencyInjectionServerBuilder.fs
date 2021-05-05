namespace Plough.WebApi.Server.Builder.DependencyInjection

open Plough.ControlFlow
open Plough.WebApi.Server

/// <summary>
/// Server builder, that contains all functions required to build Giraffe/Suave web server endpoints, used to indirect dependencies of underlying web server from core library
/// </summary>
type ServerBuilder<'ctx> =
    
    // ---------------------------
    // Dependency injection route handling functions
    // ---------------------------
    abstract member makeDownloadHandlerWithArg<'service, 'a> : download : DownloadServiceWithObject<'service, 'a> -> param : 'a -> HttpHandler<'ctx>
    abstract member makeDownloadHandlerWithArgAsync<'service, 'a> : download : DownloadServiceWithObjectAsync<'service, 'a> -> param : 'a -> HttpHandler<'ctx>
    abstract member makeDownloadHandlerNoArg<'service> : download : DownloadServiceWithObject<'service, unit> -> HttpHandler<'ctx>
    abstract member makeDownloadHandlerNoArgAsync<'service> : download : DownloadServiceWithObjectAsync<'service, unit> -> HttpHandler<'ctx>
    abstract member makeDownloadHandlerWithObj<'service, 'a> : download : DownloadServiceWithObject<'service, 'a> -> HttpHandler<'ctx>
    abstract member makeDownloadHandlerWithObjAsync<'service, 'a> : download : DownloadServiceWithObjectAsync<'service, 'a> -> HttpHandler<'ctx>
    
    abstract member makeJSONHandler<'service, 'b> : call : CallService<'service, 'b> -> HttpHandler<'ctx>
    abstract member makeJSONHandlerAsync<'service, 'b> : call : CallServiceAsync<'service, 'b> -> HttpHandler<'ctx>
    
    abstract member makeJSONHandlerWithArg<'service, 'a, 'b> : call : CallServiceWithObject<'service, 'a, 'b> -> i : 'a -> HttpHandler<'ctx>
    abstract member makeJSONHandlerWithArgAsync<'service, 'a, 'b> : call : CallServiceWithObjectAsync<'service, 'a, 'b> -> i : 'a -> HttpHandler<'ctx>
    
    abstract member makeJSONHandlerWithTwoArg<'service, 'a, 'b, 'c> : call : CallServiceWithTwoObjects<'service, 'a, 'b, 'c> -> i : 'a -> HttpHandler<'ctx>
    abstract member makeJSONHandlerWithTwoArgAsync<'service, 'a, 'b, 'c> : call : CallServiceWithTwoObjectsAsync<'service, 'a, 'b, 'c> -> i : 'a -> HttpHandler<'ctx>
    
    abstract member makeJSONHandlerWithQueryParam<'service, 'a, 'b> : call : CallServiceWithObject<'service, 'a, 'b> -> HttpHandler<'ctx>
    abstract member makeJSONHandlerWithQueryParamAsync<'service, 'a, 'b> : call : CallServiceWithObjectAsync<'service, 'a, 'b> -> HttpHandler<'ctx>
    
    abstract member makeJSONHandlerWithArgQueryParam<'service, 'a, 'b, 'c> : call : CallServiceWithTwoObjects<'service, 'a, 'b, 'c> -> i : 'a -> HttpHandler<'ctx>
    abstract member makeJSONHandlerWithArgQueryParamAsync<'service, 'a, 'b, 'c> : call : CallServiceWithTwoObjectsAsync<'service, 'a, 'b, 'c> -> i : 'a -> HttpHandler<'ctx>
    
    abstract member makeJSONHandlerWithObj<'service, 'a, 'b> : call : CallServiceWithObject<'service, 'a, 'b> -> HttpHandler<'ctx>
    abstract member makeJSONHandlerWithObjAsync<'service, 'a, 'b> : call : CallServiceWithObjectAsync<'service, 'a, 'b> -> HttpHandler<'ctx>
    
    abstract member makeJSONHandlerWithObjInt<'service, 'a, 'b> : call : CallServiceWithIntAndObject<'service, 'a, 'b> -> i : int -> HttpHandler<'ctx>
    abstract member makeJSONHandlerWithObjIntAsync<'service, 'a, 'b> : call : CallServiceWithIntAndObjectAsync<'service, 'a, 'b> -> i : int -> HttpHandler<'ctx>
    
    abstract member makeBinaryPostHandlerWithArgAsync<'service, 'a, 'b> : call : ('service -> 'a -> byte [] -> TaskEither<'b>) -> arg : 'a -> HttpHandler<'ctx>
    abstract member makeBinaryResultHandlerWithArgAsync<'service, 'a, 'b> : call : ('service -> 'a -> Task<byte []>) -> arg : 'a -> HttpHandler<'ctx>
    
[<AutoOpen>]
module Builder =

    let inline makeJSONHandler (call : CallService<'service, 'b>) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandler call
    
    let inline makeJSONHandlerAsync (call : CallServiceAsync<'service, 'b>) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerAsync call
        
    let inline makeJSONHandlerWithArg (call : CallServiceWithObject<'service, 'a, 'b>) (i : 'a) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithArg call i
        
    let inline makeJSONHandlerWithArgAsync (call : CallServiceWithObjectAsync<'service, 'a, 'b>) (i : 'a) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithArgAsync call i
       
    let inline makeJSONHandlerWithObj (call : CallServiceWithObject<'service, 'a, 'b>) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithObj call
        
    let inline makeJSONHandlerWithObjAsync (call : CallServiceWithObjectAsync<'service, 'a, 'b>) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithObjAsync call
        
    let inline makeJSONHandlerWithTwoArg (call : CallServiceWithTwoObjects<'service, 'a, 'b, 'c>) (i : 'a) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithTwoArg call i
        
    let inline makeJSONHandlerWithTwoArgAsync (call : CallServiceWithTwoObjectsAsync<'service, 'a, 'b, 'c>) (i : 'a) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithTwoArgAsync call i
        
    let inline makeJSONHandlerWithQueryParam (call : CallServiceWithObject<'service, 'a, 'b>) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithQueryParam call
        
    let inline makeJSONHandlerWithQueryParamAsync (call : CallServiceWithObjectAsync<'service, 'a, 'b>) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithQueryParamAsync call
        
    let inline makeJSONHandlerWithArgQueryParam (call : CallServiceWithTwoObjects<'service, 'a, 'b, 'c>) (i : 'a) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithArgQueryParam call i
        
    let inline makeJSONHandlerWithArgQueryParamAsync (call : CallServiceWithTwoObjectsAsync<'service, 'a, 'b, 'c>) (i : 'a) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithArgQueryParamAsync call i
        
    let inline makeJSONHandlerWithObjInt (call : CallServiceWithIntAndObject<'service, 'a, 'b>) (i : int) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithObjInt call i
        
    let inline makeJSONHandlerWithObjIntAsync (call : CallServiceWithIntAndObjectAsync<'service, 'a, 'b>) (i : int) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithObjIntAsync call i
        
    let inline makeBinaryPostHandlerWithArgAsync (call : 'service -> 'a -> byte [] -> TaskEither<'b>) (arg : 'a) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeBinaryPostHandlerWithArgAsync call arg
        
    let inline makeBinaryResultHandlerWithArgAsync (call : 'service -> 'a -> Task<byte []>) (arg : 'a) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeBinaryResultHandlerWithArgAsync call arg