namespace Plough.WebApi.Server.Plain

open Plough.ControlFlow
open Plough.WebApi.Server

/// <summary>
/// Server builder, that contains all functions required to build Giraffe/Suave web server endpoints, used to indirect dependencies of underlying web server from core library
/// </summary>
type ServerBuilder<'context> =
    
    // ---------------------------
    // Plain (no dependency injection) route handling functions
    // ---------------------------
    
    abstract member makeJSONHandler<'b> : call : Call<'b> -> HttpHandler<'context>
    abstract member makeJSONHandlerAsync<'b> : call : CallAsync<'b> -> HttpHandler<'context>
    
    abstract member makeJSONHandlerWithArg<'a, 'b> : call : CallWithObject<'a, 'b> -> i : 'a -> HttpHandler<'context>
    abstract member makeJSONHandlerWithArgAsync<'a, 'b> : call : CallWithObjectAsync<'a, 'b> -> i : 'a -> HttpHandler<'context>
    
    abstract member makeJSONHandlerWithTwoArg<'a, 'b, 'c> : call : CallWithTwoObjects<'a, 'b, 'c> -> i : 'a -> HttpHandler<'context>
    abstract member makeJSONHandlerWithTwoArgAsync<'a, 'b, 'c> : call : CallWithTwoObjectsAsync<'a, 'b, 'c> -> i : 'a -> HttpHandler<'context>
    
    abstract member makeJSONHandlerWithQueryParam<'a, 'b> : call : CallWithObject<'a, 'b> -> HttpHandler<'context>
    abstract member makeJSONHandlerWithQueryParamAsync<'a, 'b> : call : CallWithObjectAsync<'a, 'b> -> HttpHandler<'context>
    
    abstract member makeJSONHandlerWithArgQueryParam<'a, 'b, 'c> : call : CallWithTwoObjects<'a, 'b, 'c> -> i : 'a -> HttpHandler<'context>
    abstract member makeJSONHandlerWithArgQueryParamAsync<'a, 'b, 'c> : call : CallWithTwoObjectsAsync<'a, 'b, 'c> -> i : 'a -> HttpHandler<'context>
    
    abstract member makeJSONHandlerWithObj<'a, 'b> : call : CallWithObject<'a, 'b> -> HttpHandler<'context>
    abstract member makeJSONHandlerWithObjAsync<'a, 'b> : call : CallWithObjectAsync<'a, 'b> -> HttpHandler<'context>
    
    abstract member makeJSONHandlerWithObjInt<'a, 'b> : call : CallWithIntAndObject<'a, 'b> -> i : int -> HttpHandler<'context>
    abstract member makeJSONHandlerWithObjIntAsync<'a, 'b> : call : CallWithIntAndObjectAsync<'a, 'b> -> i : int -> HttpHandler<'context>
    
    abstract member makeBinaryPostHandlerWithArgAsync<'a, 'b> : call : ('a -> byte [] -> TaskEither<'b>) -> arg : 'a -> HttpHandler<'context>
    abstract member makeBinaryResultHandlerWithArgAsync<'a, 'b> : call : ('a -> Task<byte []>) -> arg : 'a -> HttpHandler<'context>
    
[<AutoOpen>]
module Builder =
    
    let inline makeJSONHandler (call : Call<'b>) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandler call
    
    let inline makeJSONHandlerAsync (call : CallAsync<'b>) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerAsync call
        
    let inline makeJSONHandlerWithArg (call : CallWithObject<'a, 'b>) (i : 'a) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithArg call i
        
    let inline makeJSONHandlerWithArgAsync (call : CallWithObjectAsync<'a, 'b>) (i : 'a) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithArgAsync call i
        
    let inline makeJSONHandlerWithTwoArg (call : CallWithTwoObjects<'a, 'b, 'c>) (i : 'a) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithTwoArg call i
        
    let inline makeJSONHandlerWithTwoArgAsync (call : CallWithTwoObjectsAsync<'a, 'b, 'c>) (i : 'a) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithTwoArgAsync call i
        
    let inline makeJSONHandlerWithQueryParam (call : CallWithObject<'a, 'b>) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithQueryParam call
        
    let inline makeJSONHandlerWithQueryParamAsync (call : CallWithObjectAsync<'a, 'b>) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithQueryParamAsync call
        
    let inline makeJSONHandlerWithArgQueryParam (call : CallWithTwoObjects<'a, 'b, 'c>) (i : 'a) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithArgQueryParam call i
        
    let inline makeJSONHandlerWithArgQueryParamAsync (call : CallWithTwoObjectsAsync<'a, 'b, 'c>) (i : 'a) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithArgQueryParamAsync call i
        
    let inline makeJSONHandlerWithObjInt (call : CallWithIntAndObject<'a, 'b>) (i : int) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithObjInt call i
        
    let inline makeJSONHandlerWithObjIntAsync (call : CallWithIntAndObjectAsync<'a, 'b>) (i : int) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeJSONHandlerWithObjIntAsync call i
        
    let inline makeBinaryPostHandlerWithArgAsync (call : 'a -> byte [] -> TaskEither<'b>) (arg : 'a) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeBinaryPostHandlerWithArgAsync call arg
        
    let inline makeBinaryResultHandlerWithArgAsync (call : 'a -> Task<byte []>) (arg : 'a) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
        fun builder -> builder.makeBinaryResultHandlerWithArgAsync call arg