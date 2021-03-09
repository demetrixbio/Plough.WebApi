namespace Plough.WebApi.Server.DependencyInjection

open Plough.ControlFlow
open Plough.WebApi.Server

/// <summary>
/// Server builder, that contains all functions required to build Giraffe/Suave web server endpoints, used to indirect dependencies of underlying web server from core library
/// </summary>
type ServerBuilder<'context> =
    
    // ---------------------------
    // Dependency injection route handling functions
    // ---------------------------
    
    abstract member makeJSONHandler<'service, 'b> : call : CallService<'service, 'b> -> HttpHandler<'context>
    abstract member makeJSONHandlerAsync<'service, 'b> : call : CallServiceAsync<'service, 'b> -> HttpHandler<'context>
    
    abstract member makeJSONHandlerWithArg<'service, 'a, 'b> : call : CallServiceWithObject<'service, 'a, 'b> -> i : 'a -> HttpHandler<'context>
    abstract member makeJSONHandlerWithArgAsync<'service, 'a, 'b> : call : CallServiceWithObjectAsync<'service, 'a, 'b> -> i : 'a -> HttpHandler<'context>
    
    abstract member makeJSONHandlerWithTwoArg<'service, 'a, 'b, 'c> : call : CallServiceWithTwoObjects<'service, 'a, 'b, 'c> -> i : 'a -> HttpHandler<'context>
    abstract member makeJSONHandlerWithTwoArgAsync<'service, 'a, 'b, 'c> : call : CallServiceWithTwoObjectsAsync<'service, 'a, 'b, 'c> -> i : 'a -> HttpHandler<'context>
    
    abstract member makeJSONHandlerWithQueryParam<'service, 'a, 'b> : call : CallServiceWithObject<'service, 'a, 'b> -> HttpHandler<'context>
    abstract member makeJSONHandlerWithQueryParamAsync<'service, 'a, 'b> : call : CallServiceWithObjectAsync<'service, 'a, 'b> -> HttpHandler<'context>
    
    abstract member makeJSONHandlerWithArgQueryParam<'service, 'a, 'b, 'c> : call : CallServiceWithTwoObjects<'service, 'a, 'b, 'c> -> i : 'a -> HttpHandler<'context>
    abstract member makeJSONHandlerWithArgQueryParamAsync<'service, 'a, 'b, 'c> : call : CallServiceWithTwoObjectsAsync<'service, 'a, 'b, 'c> -> i : 'a -> HttpHandler<'context>
    
    abstract member makeJSONHandlerWithObj<'service, 'a, 'b> : call : CallServiceWithObject<'service, 'a, 'b> -> HttpHandler<'context>
    abstract member makeJSONHandlerWithObjAsync<'service, 'a, 'b> : call : CallServiceWithObjectAsync<'service, 'a, 'b> -> HttpHandler<'context>
    
    abstract member makeJSONHandlerWithObjInt<'service, 'a, 'b> : call : CallServiceWithIntAndObject<'service, 'a, 'b> -> i : int -> HttpHandler<'context>
    abstract member makeJSONHandlerWithObjIntAsync<'service, 'a, 'b> : call : CallServiceWithIntAndObjectAsync<'service, 'a, 'b> -> i : int -> HttpHandler<'context>
    
    abstract member makeBinaryPostHandlerWithArgAsync<'service, 'a, 'b> : call : ('service -> 'a -> byte [] -> TaskEither<'b>) -> arg : 'a -> HttpHandler<'context>
    abstract member makeBinaryResultHandlerWithArgAsync<'service, 'a, 'b> : call : ('service -> 'a -> Task<byte []>) -> arg : 'a -> HttpHandler<'context>
    
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