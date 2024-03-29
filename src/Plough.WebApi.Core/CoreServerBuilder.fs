﻿namespace Plough.WebApi.Server.Builder.Core

open System.Globalization
open Plough.WebApi.Server

/// <summary>
/// Server builder, that contains all functions required to build Giraffe/Suave web server endpoints, used to indirect dependencies of underlying web server from core library
/// </summary>
type ServerBuilder<'ctx> =
    // ---------------------------
    // Globally useful functions
    // ---------------------------

    /// <summary>
    /// The warbler function is a <see cref="HttpHandler"/> wrapper function which prevents a <see cref="HttpHandler"/> to be pre-evaluated at startup.
    /// </summary>
    /// <param name="f">A function which takes a HttpFunc * HttpContext tuple and returns a <see cref="HttpHandler"/> function.</param>
    /// <param name="next"></param>
    /// <param name="ctx"></param>
    /// <example>
    /// <code>
    /// warbler(fun _ -> someHttpHandler)
    /// </code>
    /// </example>
    /// <returns>Returns a <see cref="HttpHandler"/> function.</returns>
    abstract member warbler : f : ((HttpFunc<'ctx> * 'ctx) -> HttpFunc<'ctx> -> 'ctx -> 'c) -> next : HttpFunc<'ctx> -> ctx : 'ctx -> 'c
    
    /// <summary>
    /// Use skipPipeline to shortcircuit the <see cref="HttpHandler"/> pipeline and return None to the surrounding <see cref="HttpHandler"/> or the Giraffe/Suave middleware (which would subsequently invoke the next middleware as a result of it).
    /// </summary>
    abstract member skipPipeline : HttpFuncResult<'ctx>
    
    /// <summary>
    /// Use earlyReturn to shortcircuit the <see cref="HttpHandler"/> pipeline and return Some HttpContext to the surrounding <see cref="HttpHandler"/> or the Giraffe/Suave middleware (which would subsequently end the pipeline by returning the response back to the client).
    /// </summary>
    abstract member earlyReturn : HttpFunc<'ctx>
    
    // ---------------------------
    // Convenience Handlers
    // ---------------------------

    /// <summary>
    /// The handleContext function is a convenience function which can be used to create a new <see cref="HttpHandler"/> function which only requires access to the <see cref="Microsoft.AspNetCore.Http.HttpContext"/> object.
    /// </summary>
    /// <param name="contextMap">A function which accepts a <see cref="Microsoft.AspNetCore.Http.HttpContext"/> object and returns a <see cref="HttpFuncResult"/> function.</param>
    /// <param name="next"></param>
    /// <param name="ctx"></param>
    /// <returns>A Giraffe/Suave <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member handleContext : contextMap : ('ctx -> HttpFuncResult<'ctx>) -> next : HttpFunc<'ctx> -> ctx : 'ctx -> HttpFuncResult<'ctx>
    
    // ---------------------------
    // Default Combinators
    // ---------------------------

    /// <summary>
    /// Combines two <see cref="HttpHandler"/> functions into one.
    /// Please mind that both <see cref="HttpHandler"/>  functions will get pre-evaluated at runtime by applying the next <see cref="HttpFunc"/> parameter of each handler.
    /// You can also use the fish operator `>=>` as a more convenient alternative to compose.
    /// </summary>
    /// <param name="handler1"></param>
    /// <param name="handler2"></param>
    /// <param name="final"></param>
    /// <returns>A <see cref="HttpFunc"/>.</returns>
    abstract member compose : handler1 : HttpHandler<'ctx> -> handler2 : HttpHandler<'ctx> -> final : HttpFunc<'ctx> -> HttpFunc<'ctx>
    
    /// <summary>
    /// Iterates through a list of <see cref="HttpHandler"/> functions and returns the result of the first <see cref="HttpHandler"/> of which the outcome is Some HttpContext.
    /// Please mind that all <see cref="HttpHandler"/> functions will get pre-evaluated at runtime by applying the next (HttpFunc) parameter to each handler.
    /// </summary>
    /// <param name="handlers"></param>
    /// <param name="next"></param>
    /// <returns>A <see cref="HttpFunc"/>.</returns>
    abstract member choose : handlers : HttpHandler<'ctx> list -> next : HttpFunc<'ctx> -> HttpFunc<'ctx>
    
    // ---------------------------
    // Default HttpHandlers
    // ---------------------------
    
    abstract member GET      : HttpHandler<'ctx>
    abstract member POST     : HttpHandler<'ctx>
    abstract member PUT      : HttpHandler<'ctx>
    abstract member PATCH    : HttpHandler<'ctx>
    abstract member DELETE   : HttpHandler<'ctx>
    abstract member HEAD     : HttpHandler<'ctx>
    abstract member OPTIONS  : HttpHandler<'ctx>
    abstract member TRACE    : HttpHandler<'ctx>
    abstract member CONNECT  : HttpHandler<'ctx>
    abstract member GET_HEAD : HttpHandler<'ctx>
    
    /// <summary>
    /// Clears the current <see cref="Microsoft.AspNetCore.Http.HttpResponse"/> object.
    /// This can be useful if a <see cref="HttpHandler"/> function needs to overwrite the response of all previous <see cref="HttpHandler"/> functions with its own response (most commonly used by an <see cref="ErrorHandler"/> function).
    /// </summary>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member clearResponse : HttpHandler<'ctx>
    
    /// <summary>
    /// Sets the HTTP status code of the response.
    /// </summary>
    /// <param name="statusCode">The status code to be set in the response. For convenience you can use the static <see cref="Microsoft.AspNetCore.Http.StatusCodes"/> class for passing in named status codes instead of using pure int values.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member setStatusCode : statusCode : int -> HttpHandler<'ctx>
    
    /// <summary>
    /// Adds or sets a HTTP header in the response.
    /// </summary>
    /// <param name="key">The HTTP header name. For convenience you can use the static <see cref="Microsoft.Net.Http.Headers.HeaderNames"/> class for passing in strongly typed header names instead of using pure string values.</param>
    /// <param name="value">The value to be set. Non string values will be converted to a string using the object's ToString() method.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member setHttpHeader : key : string -> value : obj -> HttpHandler<'ctx>
    
    /// <summary>
    /// Filters an incoming HTTP request based on the accepted mime types of the client (Accept HTTP header).
    /// If the client doesn't accept any of the provided mimeTypes then the handler will not continue executing the next <see cref="HttpHandler"/> function.
    /// </summary>
    /// <param name="mimeTypes">List of mime types of which the client has to accept at least one.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member mustAccept : mimeTypes : string list -> HttpHandler<'ctx>
    
    /// <summary>
    /// Redirects to a different location with a `302` or `301` (when permanent) HTTP status code.
    /// </summary>
    /// <param name="permanent">If true the redirect is permanent (301), otherwise temporary (302).</param>
    /// <param name="location">The URL to redirect the client to.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member redirectTo : permanent : bool -> location : string -> HttpHandler<'ctx>

    // ---------------------------
    // Model binding functions
    // ---------------------------

    /// <summary>
    /// Parses a JSON payload into an instance of type 'T.
    /// </summary>
    /// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member bindJson<'T> : f : ('T -> HttpHandler<'ctx>) -> HttpHandler<'ctx>
    
    /// <summary>
    /// Parses a XML payload into an instance of type 'T.
    /// </summary>
    /// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member bindXml<'T> : f : ('T -> HttpHandler<'ctx>) -> HttpHandler<'ctx>
    
    /// <summary>
    /// Parses a HTTP form payload into an instance of type 'T.
    /// </summary>
    /// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
    /// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member bindForm<'T> : culture : CultureInfo option -> f : ('T -> HttpHandler<'ctx>) -> HttpHandler<'ctx>
    
    /// <summary>
    /// Tries to parse a HTTP form payload into an instance of type 'T.
    /// </summary>
    /// <param name="parsingErrorHandler">A <see cref="System.String"/> -> <see cref="HttpHandler"/> function which will get invoked when the model parsing fails. The <see cref="System.String"/> parameter holds the parsing error message.</param>
    /// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
    /// <param name="successHandler">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member tryBindForm<'T> : parsingErrorHandler : (string -> HttpHandler<'ctx>) ->
                                      culture             : CultureInfo option ->
                                      successHandler      : ('T -> HttpHandler<'ctx>) ->
                                      HttpHandler<'ctx>
    
    /// <summary>
    /// Parses a HTTP query string into an instance of type 'T.
    /// </summary>
    /// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
    /// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member bindQuery<'T> : culture : CultureInfo option -> f : ('T -> HttpHandler<'ctx>) -> HttpHandler<'ctx>
    
    /// <summary>
    /// Tries to parse a query string into an instance of type `'T`.
    /// </summary>
    /// <param name="parsingErrorHandler">A <see href="HttpHandler"/> function which will get invoked when the model parsing fails. The <see cref="System.String"/> input parameter holds the parsing error message.</param>
    /// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
    /// <param name="successHandler">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member tryBindQuery<'T> : parsingErrorHandler : (string -> HttpHandler<'ctx>) ->
                                       culture             : CultureInfo option ->
                                       successHandler      : ('T -> HttpHandler<'ctx>) ->
                                       HttpHandler<'ctx>
    
    /// <summary>
    /// Parses a HTTP payload into an instance of type 'T.
    /// The model can be sent via XML, JSON, form or query string.
    /// </summary>
    /// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
    /// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member bindModel<'T> : culture : CultureInfo option -> f : ('T -> HttpHandler<'ctx>) -> HttpHandler<'ctx>
    
    // ---------------------------
    // Response writing functions
    // ---------------------------

    /// **Description**
    ///
    /// Writes a byte array to the body of the HTTP response and sets the HTTP `Content-Length` header accordingly.
    ///
    /// **Parameters**
    ///
    /// `bytes`: The byte array to be send back to the client.
    ///
    /// **Output**
    ///
    /// A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.

    /// <summary>
    /// Writes a byte array to the body of the HTTP response and sets the HTTP Content-Length header accordingly.
    /// </summary>
    /// <param name="bytes">The byte array to be send back to the client.</param>
    /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
    abstract member setBody : bytes : byte array -> HttpHandler<'ctx>
    
    /// <summary>
    /// Writes an UTF-8 encoded string to the body of the HTTP response and sets the HTTP Content-Length header accordingly.
    /// </summary>
    /// <param name="str">The string value to be send back to the client.</param>
    /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
    abstract member setBodyFromString : str : string -> HttpHandler<'ctx>
    
    /// <summary>
    /// Writes an UTF-8 encoded string to the body of the HTTP response and sets the HTTP Content-Length header accordingly, as well as the Content-Type header to text/plain.
    /// </summary>
    /// <param name="str">The string value to be send back to the client.</param>
    /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
    abstract member text : str : string -> HttpHandler<'ctx>
    
    /// <summary>
    /// Serializes an object to JSON and writes the output to the body of the HTTP response.
    /// It also sets the HTTP Content-Type header to application/json and sets the Content-Length header accordingly.
    /// The JSON serializer can be configured in the ASP.NET Core startup code by registering a custom class of type <see cref="Json.ISerializer"/>.
    /// </summary>
    /// <param name="dataObj">The object to be send back to the client.</param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
    abstract member json<'T> : dataObj : 'T -> HttpHandler<'ctx>
    
    /// <summary>
    /// Serializes an object to JSON and writes the output to the body of the HTTP response using chunked transfer encoding.
    /// It also sets the HTTP Content-Type header to application/json and sets the Transfer-Encoding header to chunked.
    /// The JSON serializer can be configured in the ASP.NET Core startup code by registering a custom class of type <see cref="Json.ISerializer"/>.
    /// </summary>
    /// <param name="dataObj">The object to be send back to the client.</param>
    /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
    abstract member jsonChunked<'T> : dataObj : 'T -> HttpHandler<'ctx>
    
    /// <summary>
    /// Serializes an object to XML and writes the output to the body of the HTTP response.
    /// It also sets the HTTP Content-Type header to application/xml and sets the Content-Length header accordingly.
    /// The JSON serializer can be configured in the ASP.NET Core startup code by registering a custom class of type <see cref="Xml.ISerializer"/>.
    /// </summary>
    /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
    abstract member xml : dataObj : obj -> HttpHandler<'ctx>
    
    /// <summary>
    /// Reads a HTML file from disk and writes its contents to the body of the HTTP response.
    /// It also sets the HTTP header Content-Type to text/html and sets the Content-Length header accordingly.
    /// </summary>
    /// <param name="filePath">A relative or absolute file path to the HTML file.</param>
    /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
    abstract member htmlFile : filePath : string -> HttpHandler<'ctx>
    
    /// <summary>
    /// Writes a HTML string to the body of the HTTP response.
    /// It also sets the HTTP header Content-Type to text/html and sets the Content-Length header accordingly.
    /// </summary>
    /// <param name="html">The HTML string to be send back to the client.</param>
    /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
    abstract member htmlString : html : string -> HttpHandler<'ctx>
    
    // ---------------------------
    // Routing functions
    // ---------------------------
    
    /// <summary>
    /// Filters an incoming HTTP request based on the port.
    /// </summary>
    /// <param name="fns">List of port to <see cref="HttpHandler"/> mappings</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member routePorts : fns : (int * HttpHandler<'ctx>) list -> HttpHandler<'ctx>
    
    /// <summary>
    /// Filters an incoming HTTP request based on the request path (case sensitive).
    /// </summary>
    /// <param name="path">Request path.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member route : path : string -> HttpHandler<'ctx>
    
    /// <summary>
    /// Filters an incoming HTTP request based on the request path (case insensitive).
    /// </summary>
    /// <param name="path">Request path.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member routeCi : path : string -> HttpHandler<'ctx>
    
    /// <summary>
    /// Filters an incoming HTTP request based on the request path using Regex (case sensitive).
    /// </summary>
    /// <param name="path">Regex path.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member routex : path : string -> HttpHandler<'ctx>
 
    /// <summary>
    /// Filters an incoming HTTP request based on the request path using Regex (case insensitive).
    /// </summary>
    /// <param name="path">Regex path.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member routeCix : path : string -> HttpHandler<'ctx>
    
    /// <summary>
    /// Filters an incoming HTTP request based on the request path (case sensitive).
    /// If the route matches the incoming HTTP request then the arguments from the <see cref="Microsoft.FSharp.Core.PrintfFormat"/> will be automatically resolved and passed into the supplied routeHandler.
    ///
    /// Supported format chars**
    ///
    /// %b: bool
    /// %c: char
    /// %s: string
    /// %i: int
    /// %d: int64
    /// %f: float/double
    /// %O: Guid
    /// </summary>
    /// <param name="path">A format string representing the expected request path.</param>
    /// <param name="routeHandler">A function which accepts a tuple 'T of the parsed arguments and returns a <see cref="HttpHandler"/> function which will subsequently deal with the request.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member routef : path : PrintfFormat<_,_,_,_, 'T> -> routeHandler : ('T -> HttpHandler<'ctx>) -> HttpHandler<'ctx>
    
    /// <summary>
    /// Filters an incoming HTTP request based on the request path.
    /// If the route matches the incoming HTTP request then the arguments from the <see cref="Microsoft.FSharp.Core.PrintfFormat"/> will be automatically resolved and passed into the supplied routeHandler.
    ///
    /// Supported format chars**
    ///
    /// %b: bool
    /// %c: char
    /// %s: string
    /// %i: int
    /// %d: int64
    /// %f: float/double
    /// %O: Guid
    /// </summary>
    /// <param name="path">A format string representing the expected request path.</param>
    /// <param name="routeHandler">A function which accepts a tuple 'T of the parsed arguments and returns a <see cref="HttpHandler"/> function which will subsequently deal with the request.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member routeCif : path : PrintfFormat<_,_,_,_, 'T> -> routeHandler : ('T -> HttpHandler<'ctx>) -> HttpHandler<'ctx>

    /// <summary>
    /// Filters an incoming HTTP request based on the request path (case insensitive).
    /// If the route matches the incoming HTTP request then the parameters from the string will be used to create an instance of 'T and subsequently passed into the supplied routeHandler.
    /// </summary>
    /// <param name="route">A string representing the expected request path. Use {propertyName} for reserved parameter names which should map to the properties of type 'T. You can also use valid Regex within the route string.</param>
    /// <param name="routeHandler">A function which accepts a tuple 'T of the parsed parameters and returns a <see cref="HttpHandler"/> function which will subsequently deal with the request.</param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member routeBind<'T> : route : string -> routeHandler : ('T -> HttpHandler<'ctx>) -> HttpHandler<'ctx>
    
    /// <summary>
    /// Filters an incoming HTTP request based on the beginning of the request path (case sensitive).
    /// </summary>
    /// <param name="subPath">The expected beginning of a request path.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member routeStartsWith : subPath : string -> HttpHandler<'ctx>
    
    /// <summary>
    /// Filters an incoming HTTP request based on the beginning of the request path (case insensitive).
    /// </summary>
    /// <param name="subPath">The expected beginning of a request path.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member routeStartsWithCi : subPath : string -> HttpHandler<'ctx>
    
    /// <summary>
    /// Filters an incoming HTTP request based on the beginning of the request path (case sensitive).
    /// If the route matches the incoming HTTP request then the arguments from the <see cref="Microsoft.FSharp.Core.PrintfFormat"/> will be automatically resolved and passed into the supplied routeHandler.
    ///
    /// Supported format chars**
    ///
    /// %b: bool
    /// %c: char
    /// %s: string
    /// %i: int
    /// %d: int64
    /// %f: float/double
    /// %O: Guid
    /// </summary>
    /// <param name="path">A format string representing the expected request path.</param>
    /// <param name="routeHandler">A function which accepts a tuple 'T of the parsed arguments and returns a <see cref="HttpHandler"/> function which will subsequently deal with the request.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member routeStartsWithf : path : PrintfFormat<_,_,_,_, 'T> -> routeHandler : ('T -> HttpHandler<'ctx>) -> HttpHandler<'ctx>
    
    /// <summary>
    /// Filters an incoming HTTP request based on the beginning of the request path (case insensitive).
    /// If the route matches the incoming HTTP request then the arguments from the <see cref="Microsoft.FSharp.Core.PrintfFormat"/> will be automatically resolved and passed into the supplied routeHandler.
    ///
    /// Supported format chars**
    ///
    /// %b: bool
    /// %c: char
    /// %s: string
    /// %i: int
    /// %d: int64
    /// %f: float/double
    /// %O: Guid
    /// </summary>
    /// <param name="path">A format string representing the expected request path.</param>
    /// <param name="routeHandler">A function which accepts a tuple 'T of the parsed arguments and returns a <see cref="HttpHandler"/> function which will subsequently deal with the request.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member routeStartsWithCif : path : PrintfFormat<_,_,_,_, 'T> -> routeHandler : ('T -> HttpHandler<'ctx>) -> HttpHandler<'ctx>
    
    /// <summary>
    /// Filters an incoming HTTP request based on a part of the request path (case sensitive).
    /// Subsequent route handlers inside the given handler function should omit the already validated path.
    /// </summary>
    /// <param name="path">A part of an expected request path.</param>
    /// <param name="handler">A Giraffe <see cref="HttpHandler"/> function.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member subRoute : path : string -> handler : HttpHandler<'ctx> -> HttpHandler<'ctx>
    
    /// <summary>
    /// Filters an incoming HTTP request based on a part of the request path (case insensitive).
    /// Subsequent route handlers inside the given handler function should omit the already validated path.
    /// </summary>
    /// <param name="path">A part of an expected request path.</param>
    /// <param name="handler">A Giraffe <see cref="HttpHandler"/> function.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member subRouteCi : path : string -> handler : HttpHandler<'ctx> -> HttpHandler<'ctx>

    /// <summary>
    /// Filters an incoming HTTP request based on a part of the request path (case sensitive).
    /// If the sub route matches the incoming HTTP request then the arguments from the <see cref="Microsoft.FSharp.Core.PrintfFormat"/> will be automatically resolved and passed into the supplied routeHandler.
    ///
    /// Supported format chars
    ///
    /// %b: bool
    /// %c: char
    /// %s: string
    /// %i: int
    /// %d: int64
    /// %f: float/double
    /// %O: Guid
    ///
    /// Subsequent routing handlers inside the given handler function should omit the already validated path.
    /// </summary>
    /// <param name="path">A format string representing the expected request sub path.</param>
    /// <param name="routeHandler">A function which accepts a tuple 'T of the parsed arguments and returns a <see cref="HttpHandler"/> function which will subsequently deal with the request.</param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    abstract member subRoutef : path : PrintfFormat<_,_,_,_, 'T> -> routeHandler : ('T -> HttpHandler<'ctx>) -> HttpHandler<'ctx>