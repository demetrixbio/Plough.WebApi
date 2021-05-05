namespace Plough.WebApi.Server

open System.Globalization
open Plough.ControlFlow
open Plough.WebApi.Server
open Plough.WebApi.Server.Builder

type ServerBuilder<'ctx> =
    inherit Core.ServerBuilder<'ctx>
    inherit Auth.ServerBuilder<'ctx>
    inherit Plain.ServerBuilder<'ctx>
    inherit DependencyInjection.ServerBuilder<'ctx>
    
    
type ApiServer<'ctx> = ServerBuilder<'ctx> -> HttpHandler<'ctx>
type HttpHandlerPromise<'ctx> = (ServerBuilder<'ctx> -> HttpHandler<'ctx>)
    
[<AutoOpen>]
module Core =

    // ---------------------------
    // Globally useful functions
    // ---------------------------

    /// <summary>
    /// The warbler function is a <see cref="HttpHandler"/> wrapper function which prevents a <see cref="HttpHandler"/> to be pre-evaluated at startup.
    /// </summary>
    /// <param name="f">A function which takes a HttpFunc * HttpContext tuple and returns a <see cref="HttpHandler"/> function.</param>
    /// <param name="next"></param>
    /// <param name="ctx"></param>
    /// <param name="builder"></param>
    /// <example>
    /// <code>
    /// warbler(fun _ -> someHttpHandler)
    /// </code>
    /// </example>
    /// <returns>Returns a <see cref="HttpHandler"/> function.</returns>
    let inline warbler f (next : HttpFunc<'ctx>) (ctx : 'ctx) (builder : ServerBuilder<'ctx>) =
        builder.warbler f next ctx

    /// <summary>
    /// Use skipPipeline to shortcircuit the <see cref="HttpHandler"/> pipeline and return None to the surrounding <see cref="HttpHandler"/> or the Giraffe middleware (which would subsequently invoke the next middleware as a result of it).
    /// </summary>
    let inline skipPipeline (builder : ServerBuilder<'ctx>) : HttpFuncResult<'ctx> =
        builder.skipPipeline

    /// <summary>
    /// Use earlyReturn to shortcircuit the <see cref="HttpHandler"/> pipeline and return Some HttpContext to the surrounding <see cref="HttpHandler"/> or the Giraffe middleware (which would subsequently end the pipeline by returning the response back to the client).
    /// </summary>
    let inline earlyReturn (builder : ServerBuilder<'ctx>) : HttpFunc<'ctx> =
        builder.earlyReturn

    // ---------------------------
    // Convenience Handlers
    // ---------------------------

    /// <summary>
    /// The handleContext function is a convenience function which can be used to create a new <see cref="HttpHandler"/> function which only requires access to the <see cref="Microsoft.AspNetCore.Http.HttpContext"/> object.
    /// </summary>
    /// <param name="contextMap">A function which accepts a <see cref="Microsoft.AspNetCore.Http.HttpContext"/> object and returns a <see cref="HttpFuncResult"/> function.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline handleContext (contextMap : 'ctx -> HttpFuncResult<'ctx>) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.handleContext contextMap

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
    /// <param name="builder"></param>
    /// <returns>A <see cref="HttpFunc"/>.</returns>
    let inline compose (handler1 : HttpHandlerPromise<'ctx>) (handler2 : HttpHandlerPromise<'ctx>) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.compose (handler1 builder) (handler2 builder)

    /// <summary>
    /// Combines two <see cref="HttpHandler"/> functions into one.
    /// Please mind that both <see cref="HttpHandler"/> functions will get pre-evaluated at runtime by applying the next <see cref="HttpFunc"/> parameter of each handler.
    /// </summary>
    let (>=>) = compose

    /// <summary>
    /// Iterates through a list of <see cref="HttpHandler"/> functions and returns the result of the first <see cref="HttpHandler"/> of which the outcome is Some HttpContext.
    /// Please mind that all <see cref="HttpHandler"/> functions will get pre-evaluated at runtime by applying the next (HttpFunc) parameter to each handler.
    /// </summary>
    /// <param name="handlers"></param>
    /// <param name="builder"></param>
    /// <returns>A <see cref="HttpFunc"/>.</returns>
    let inline choose (handlers : HttpHandlerPromise<'ctx> list) : HttpHandlerPromise<'ctx> =
        fun builder ->
            handlers
            |> List.map (fun f -> f builder)
            |> builder.choose
        
    // ---------------------------
    // Default HttpHandlers
    // ---------------------------

    let inline GET      (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> = builder.GET
    let inline POST     (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> = builder.POST
    let inline PUT      (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> = builder.PUT
    let inline PATCH    (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> = builder.PATCH
    let inline DELETE   (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> = builder.DELETE
    let inline HEAD     (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> = builder.HEAD
    let inline OPTIONS  (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> = builder.OPTIONS
    let inline TRACE    (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> = builder.TRACE
    let inline CONNECT  (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> = builder.CONNECT
    let inline GET_HEAD (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> = builder.GET_HEAD

    /// <summary>
    /// Clears the current <see cref="Microsoft.AspNetCore.Http.HttpResponse"/> object.
    /// This can be useful if a <see cref="HttpHandler"/> function needs to overwrite the response of all previous <see cref="HttpHandler"/> functions with its own response (most commonly used by an <see cref="ErrorHandler"/> function).
    /// </summary>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline clearResponse (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> =
        builder.clearResponse

    /// <summary>
    /// Sets the HTTP status code of the response.
    /// </summary>
    /// <param name="statusCode">The status code to be set in the response. For convenience you can use the static <see cref="Microsoft.AspNetCore.Http.StatusCodes"/> class for passing in named status codes instead of using pure int values.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline setStatusCode (statusCode : int) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.setStatusCode statusCode
        
    /// <summary>
    /// Adds or sets a HTTP header in the response.
    /// </summary>
    /// <param name="key">The HTTP header name. For convenience you can use the static <see cref="Microsoft.Net.Http.Headers.HeaderNames"/> class for passing in strongly typed header names instead of using pure string values.</param>
    /// <param name="value">The value to be set. Non string values will be converted to a string using the object's ToString() method.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline setHttpHeader (key : string) (value : obj) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.setHttpHeader key value

    /// <summary>
    /// Filters an incoming HTTP request based on the accepted mime types of the client (Accept HTTP header).
    /// If the client doesn't accept any of the provided mimeTypes then the handler will not continue executing the next <see cref="HttpHandler"/> function.
    /// </summary>
    /// <param name="mimeTypes">List of mime types of which the client has to accept at least one.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline mustAccept (mimeTypes : string list) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.mustAccept mimeTypes

    /// <summary>
    /// Redirects to a different location with a `302` or `301` (when permanent) HTTP status code.
    /// </summary>
    /// <param name="permanent">If true the redirect is permanent (301), otherwise temporary (302).</param>
    /// <param name="location">The URL to redirect the client to.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline redirectTo (permanent : bool) (location : string) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.redirectTo permanent location

    // ---------------------------
    // Model binding functions
    // ---------------------------

    /// <summary>
    /// Parses a JSON payload into an instance of type 'T.
    /// </summary>
    /// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
    /// <param name="builder"></param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline bindJson(f : 'T -> HttpHandlerPromise<'ctx>) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.bindJson<'T> (fun s -> f s builder)

    /// <summary>
    /// Parses a XML payload into an instance of type 'T.
    /// </summary>
    /// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
    /// <param name="builder"></param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline bindXml (f : 'T -> HttpHandler<'ctx>) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.bindXml<'T> f

    /// <summary>
    /// Parses a HTTP form payload into an instance of type 'T.
    /// </summary>
    /// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
    /// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
    /// <param name="builder"></param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline bindForm (culture : CultureInfo option) (f : 'T -> HttpHandlerPromise<'ctx>) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.bindForm<'T> culture (fun x -> f x builder)

    /// <summary>
    /// Tries to parse a HTTP form payload into an instance of type 'T.
    /// </summary>
    /// <param name="parsingErrorHandler">A <see cref="System.String"/> -> <see cref="HttpHandler"/> function which will get invoked when the model parsing fails. The <see cref="System.String"/> parameter holds the parsing error message.</param>
    /// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
    /// <param name="successHandler">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
    /// <param name="builder"></param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline tryBindForm (parsingErrorHandler : string -> HttpHandlerPromise<'ctx>)
                           (culture             : CultureInfo option)
                           (successHandler      : 'T -> HttpHandlerPromise<'ctx>)
                           : HttpHandlerPromise<'ctx> =
        fun builder -> builder.tryBindForm<'T> (fun s -> parsingErrorHandler s builder) culture (fun s -> successHandler s builder)

    /// <summary>
    /// Parses a HTTP query string into an instance of type 'T.
    /// </summary>
    /// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
    /// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
    /// <param name="builder"></param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline bindQuery (culture : CultureInfo option) (f : 'T -> HttpHandlerPromise<'ctx>) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.bindQuery<'T> culture (fun s -> f s builder)

    /// <summary>
    /// Tries to parse a query string into an instance of type `'T`.
    /// </summary>
    /// <param name="parsingErrorHandler">A <see href="HttpHandler"/> function which will get invoked when the model parsing fails. The <see cref="System.String"/> input parameter holds the parsing error message.</param>
    /// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
    /// <param name="successHandler">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
    /// <param name="builder"></param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline tryBindQuery (parsingErrorHandler : string -> HttpHandlerPromise<'ctx>)
                            (culture             : CultureInfo option)
                            (successHandler      : 'T -> HttpHandlerPromise<'ctx>)
                            : HttpHandlerPromise<'ctx> =
        fun builder -> builder.tryBindQuery<'T> (fun s -> parsingErrorHandler s builder) culture (fun s -> successHandler s builder)

    /// <summary>
    /// Parses a HTTP payload into an instance of type 'T.
    /// The model can be sent via XML, JSON, form or query string.
    /// </summary>
    /// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
    /// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
    /// <param name="builder"></param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline bindModel (culture : CultureInfo option) (f : 'T -> HttpHandlerPromise<'ctx>) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.bindModel<'T> culture (fun s -> f s builder)

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
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
    let inline setBody (bytes : byte array) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.setBody bytes

    /// <summary>
    /// Writes an UTF-8 encoded string to the body of the HTTP response and sets the HTTP Content-Length header accordingly.
    /// </summary>
    /// <param name="str">The string value to be send back to the client.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
    let inline setBodyFromString (str : string) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.setBodyFromString str

    /// <summary>
    /// Writes an UTF-8 encoded string to the body of the HTTP response and sets the HTTP Content-Length header accordingly, as well as the Content-Type header to text/plain.
    /// </summary>
    /// <param name="str">The string value to be send back to the client.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
    let inline text (str : string) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.text str

    /// <summary>
    /// Serializes an object to JSON and writes the output to the body of the HTTP response.
    /// It also sets the HTTP Content-Type header to application/json and sets the Content-Length header accordingly.
    /// The JSON serializer can be configured in the ASP.NET Core startup code by registering a custom class of type <see cref="Json.ISerializer"/>.
    /// </summary>
    /// <param name="dataObj">The object to be send back to the client.</param>
    /// <param name="builder"></param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
    let inline json (dataObj : 'T) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.json dataObj

    /// <summary>
    /// Serializes an object to JSON and writes the output to the body of the HTTP response using chunked transfer encoding.
    /// It also sets the HTTP Content-Type header to application/json and sets the Transfer-Encoding header to chunked.
    /// The JSON serializer can be configured in the ASP.NET Core startup code by registering a custom class of type <see cref="Json.ISerializer"/>.
    /// </summary>
    /// <param name="dataObj">The object to be send back to the client.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
    let inline jsonChunked (dataObj : 'T) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.jsonChunked dataObj

    /// <summary>
    /// Serializes an object to XML and writes the output to the body of the HTTP response.
    /// It also sets the HTTP Content-Type header to application/xml and sets the Content-Length header accordingly.
    /// The JSON serializer can be configured in the ASP.NET Core startup code by registering a custom class of type <see cref="Xml.ISerializer"/>.
    /// </summary>
    /// <param name="dataObj">The object to be send back to the client.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
    let inline xml (dataObj : obj) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.xml dataObj

    /// <summary>
    /// Reads a HTML file from disk and writes its contents to the body of the HTTP response.
    /// It also sets the HTTP header Content-Type to text/html and sets the Content-Length header accordingly.
    /// </summary>
    /// <param name="filePath">A relative or absolute file path to the HTML file.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
    let inline htmlFile (filePath : string) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.htmlFile filePath

    /// <summary>
    /// Writes a HTML string to the body of the HTTP response.
    /// It also sets the HTTP header Content-Type to text/html and sets the Content-Length header accordingly.
    /// </summary>
    /// <param name="html">The HTML string to be send back to the client.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
    let inline htmlString (html : string) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.htmlString html
        
    // ---------------------------
    // Routing functions
    // ---------------------------
    /// <summary>
    /// Filters an incoming HTTP request based on the port.
    /// </summary>
    /// <param name="fns">List of port to <see cref="HttpHandler"/> mappings</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline routePorts (fns : (int * HttpHandlerPromise<'ctx>) list) : HttpHandlerPromise<'ctx> =
        fun builder -> fns |> List.map (fun (i, p) -> i, p builder) |> builder.routePorts

    /// <summary>
    /// Filters an incoming HTTP request based on the request path (case sensitive).
    /// </summary>
    /// <param name="path">Request path.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline route (path : string) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.route path

    /// <summary>
    /// Filters an incoming HTTP request based on the request path (case insensitive).
    /// </summary>
    /// <param name="path">Request path.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline routeCi (path : string) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.routeCi path

    /// <summary>
    /// Filters an incoming HTTP request based on the request path using Regex (case sensitive).
    /// </summary>
    /// <param name="path">Regex path.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline routex (path : string) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.routex path

    /// <summary>
    /// Filters an incoming HTTP request based on the request path using Regex (case insensitive).
    /// </summary>
    /// <param name="path">Regex path.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline routeCix (path : string) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.routeCix path

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
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline routef (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandlerPromise<'ctx>) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.routef path (fun s -> routeHandler s builder)

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
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline routeCif (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandlerPromise<'ctx>) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.routeCif path (fun s -> routeHandler s builder)

    /// <summary>
    /// Filters an incoming HTTP request based on the request path (case insensitive).
    /// If the route matches the incoming HTTP request then the parameters from the string will be used to create an instance of 'T and subsequently passed into the supplied routeHandler.
    /// </summary>
    /// <param name="route">A string representing the expected request path. Use {propertyName} for reserved parameter names which should map to the properties of type 'T. You can also use valid Regex within the route string.</param>
    /// <param name="routeHandler">A function which accepts a tuple 'T of the parsed parameters and returns a <see cref="HttpHandler"/> function which will subsequently deal with the request.</param>
    /// <param name="builder"></param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline routeBind (route : string) (routeHandler : 'T -> HttpHandlerPromise<'ctx>) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.routeBind route (fun s -> routeHandler s builder)

    /// <summary>
    /// Filters an incoming HTTP request based on the beginning of the request path (case sensitive).
    /// </summary>
    /// <param name="subPath">The expected beginning of a request path.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline routeStartsWith (subPath : string) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.routeStartsWith subPath

    /// <summary>
    /// Filters an incoming HTTP request based on the beginning of the request path (case insensitive).
    /// </summary>
    /// <param name="subPath">The expected beginning of a request path.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let inline routeStartsWithCi (subPath : string) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.routeStartsWithCi subPath

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
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let routeStartsWithf (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandlerPromise<'ctx>) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.routeStartsWithf path (fun s -> routeHandler s builder)

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
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let routeStartsWithCif (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandlerPromise<'ctx>) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.routeStartsWithCif path (fun s -> routeHandler s builder)

    /// <summary>
    /// Filters an incoming HTTP request based on a part of the request path (case sensitive).
    /// Subsequent route handlers inside the given handler function should omit the already validated path.
    /// </summary>
    /// <param name="path">A part of an expected request path.</param>
    /// <param name="handler">A Giraffe <see cref="HttpHandler"/> function.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let subRoute (path : string) (handler : HttpHandlerPromise<'ctx>) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.subRoute path (handler builder)

    /// <summary>
    /// Filters an incoming HTTP request based on a part of the request path (case insensitive).
    /// Subsequent route handlers inside the given handler function should omit the already validated path.
    /// </summary>
    /// <param name="path">A part of an expected request path.</param>
    /// <param name="handler">A Giraffe <see cref="HttpHandler"/> function.</param>
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let subRouteCi (path : string) (handler : HttpHandlerPromise<'ctx>) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.subRouteCi path (handler builder)

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
    /// <param name="builder"></param>
    /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
    let subRoutef (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandlerPromise<'ctx>) : HttpHandlerPromise<'ctx> =
        fun builder -> builder.subRoutef path (fun s -> routeHandler s builder)

[<AutoOpen>]    
module Auth =
    
    let inline isOffline (builder : ServerBuilder<'ctx>) : bool =
        builder.isOffline
    
    let inline authenticate (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> =
        builder.authenticate
    
    let inline isLoggedIn (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> =
        builder.isLoggedIn
        
    let inline authenticateJSON (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> =
        builder.authenticateJSON
        
    let inline login (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> =
        builder.login
        
    let inline logout (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> =
        builder.logout
        
    let inline requirePolicy (policy : string) (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> =
        builder.requirePolicy policy
        
    let inline identityClaims (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> =
        builder.identityClaims
        
    let inline identity (builder : ServerBuilder<'ctx>) : HttpHandler<'ctx> =
        builder.identity

    module Endpoints =
        let authenticationEndpoints (urls : AuthUrls) = choose [
            // Basic true/false check for logged in status, always accessible
            route urls.IsLoggedIn >=> isLoggedIn
            // Clear the logged-in status for the given session
            route urls.Logout >=> logout
            // The cognito authentication handler.  Usually redirects to a Google site
            // with a bunch of extra metadata, then directs back on success.
            route urls.Login >=> login
            // Get JSON record describing currently logged-in user
            route urls.Identity >=> authenticateJSON >=> identity
            // Debug page that spits out all the claims for the currently authenticated user
            route urls.Claims >=> authenticate >=> identityClaims
        ]
        
module Plain =
    
    let inline makeDownloadHandlerWithArg<'a, 'ctx> (call : DownloadWithObject<'a>) (i : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeDownloadHandlerWithArg call i
    
    let inline makeDownloadHandlerWithArgAsync<'a, 'ctx> (call : DownloadWithObjectAsync<'a>) (i : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeDownloadHandlerWithArgAsync call i
    
    let inline makeDownloadHandlerNoArg<'ctx> (call : DownloadWithObject<unit>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeDownloadHandlerNoArg call
        
    let inline makeDownloadHandlerNoArgAsync<'ctx> (call : DownloadWithObjectAsync<unit>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeDownloadHandlerNoArgAsync call
    
    let inline makeDownloadHandlerWithObj<'a, 'ctx> (call : DownloadWithObject<'a>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeDownloadHandlerWithObj call
    
    let inline makeDownloadHandlerWithObjAsync<'a, 'ctx> (call : DownloadWithObjectAsync<'a>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeDownloadHandlerWithObjAsync call
    
    let inline makeJSONHandler<'b, 'ctx> (call : Call<'b>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeJSONHandler call
    
    let inline makeJSONHandlerAsync<'b, 'ctx> (call : CallAsync<'b>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeJSONHandlerAsync call
        
    let inline makeJSONHandlerWithObj<'a, 'b, 'ctx> (call : CallWithObject<'a, 'b>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeJSONHandlerWithObj call
    
    let inline makeJSONHandlerWithObjAsync<'a, 'b, 'ctx> (call : CallWithObjectAsync<'a, 'b>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeJSONHandlerWithObjAsync call
        
    let inline makeJSONHandlerWithArg<'a, 'b, 'ctx> (call : CallWithObject<'a, 'b>) (i : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeJSONHandlerWithArg call i
        
    let inline makeJSONHandlerWithArgAsync<'a, 'b, 'ctx> (call : CallWithObjectAsync<'a, 'b>) (i : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeJSONHandlerWithArgAsync call i
        
    let inline makeJSONHandlerWithTwoArg<'a, 'b, 'c, 'ctx> (call : CallWithTwoObjects<'a, 'b, 'c>) (i : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeJSONHandlerWithTwoArg call i
        
    let inline makeJSONHandlerWithTwoArgAsync<'a, 'b, 'c, 'ctx> (call : CallWithTwoObjectsAsync<'a, 'b, 'c>) (i : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeJSONHandlerWithTwoArgAsync call i
        
    let inline makeJSONHandlerWithQueryParam<'a, 'b, 'ctx> (call : CallWithObject<'a, 'b>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeJSONHandlerWithQueryParam call
        
    let inline makeJSONHandlerWithQueryParamAsync<'a, 'b, 'ctx> (call : CallWithObjectAsync<'a, 'b>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeJSONHandlerWithQueryParamAsync call
        
    let inline makeJSONHandlerWithArgQueryParam<'a, 'b, 'c, 'ctx> (call : CallWithTwoObjects<'a, 'b, 'c>) (i : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeJSONHandlerWithArgQueryParam call i
        
    let inline makeJSONHandlerWithArgQueryParamAsync<'a, 'b, 'c, 'ctx> (call : CallWithTwoObjectsAsync<'a, 'b, 'c>) (i : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeJSONHandlerWithArgQueryParamAsync call i
        
    let inline makeJSONHandlerWithObjInt<'a, 'b, 'ctx> (call : CallWithIntAndObject<'a, 'b>) (i : int) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeJSONHandlerWithObjInt call i
        
    let inline makeJSONHandlerWithObjIntAsync<'a, 'b, 'ctx> (call : CallWithIntAndObjectAsync<'a, 'b>) (i : int) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeJSONHandlerWithObjIntAsync call i
        
    let inline makeBinaryPostHandlerWithArgAsync<'a, 'b, 'ctx> (call : 'a -> byte [] -> TaskEither<'b>) (arg : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeBinaryPostHandlerWithArgAsync call arg
        
    let inline makeBinaryResultHandlerWithArgAsync<'a, 'ctx> (call : 'a -> Task<byte []>) (arg : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.Plain.ServerBuilder<'ctx>).makeBinaryResultHandlerWithArgAsync call arg

[<AutoOpen>] 
module DependencyInjection =
    
    let inline makeDownloadHandlerWithArg<'service, 'a, 'ctx> (call : DownloadServiceWithObject<'service, 'a>) (i : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeDownloadHandlerWithArg call i
    
    let inline makeDownloadHandlerWithArgAsync<'service, 'a, 'ctx> (call : DownloadServiceWithObjectAsync<'service, 'a>) (i : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeDownloadHandlerWithArgAsync call i
    
    let inline makeDownloadHandlerNoArg<'service, 'ctx> (call : DownloadServiceWithObject<'service, unit>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeDownloadHandlerNoArg call
        
    let inline makeDownloadHandlerNoArgAsync<'service, 'ctx> (call : DownloadServiceWithObjectAsync<'service, unit>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeDownloadHandlerNoArgAsync call
    
    let inline makeDownloadHandlerWithObj<'service, 'a, 'ctx> (call : DownloadServiceWithObject<'service, 'a>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeDownloadHandlerWithObj call
    
    let inline makeDownloadHandlerWithObjAsync<'service, 'a, 'ctx> (call : DownloadServiceWithObjectAsync<'service, 'a>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeDownloadHandlerWithObjAsync call
    
    let inline makeJSONHandler<'service, 'b, 'ctx> (call : CallService<'service, 'b>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeJSONHandler call
    
    let inline makeJSONHandlerAsync<'service, 'b, 'ctx> (call : CallServiceAsync<'service, 'b>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeJSONHandlerAsync call
        
    let inline makeJSONHandlerWithObj<'service, 'a, 'b, 'ctx> (call : CallServiceWithObject<'service, 'a, 'b>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeJSONHandlerWithObj call
    
    let inline makeJSONHandlerWithObjAsync<'service, 'a, 'b, 'ctx> (call : CallServiceWithObjectAsync<'service, 'a, 'b>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeJSONHandlerWithObjAsync call
        
    let inline makeJSONHandlerWithArg<'service, 'a, 'b, 'ctx> (call : CallServiceWithObject<'service, 'a, 'b>) (i : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeJSONHandlerWithArg call i
        
    let inline makeJSONHandlerWithArgAsync<'service, 'a, 'b, 'ctx> (call : CallServiceWithObjectAsync<'service, 'a, 'b>) (i : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeJSONHandlerWithArgAsync call i
        
    let inline makeJSONHandlerWithTwoArg<'service, 'a, 'b, 'c, 'ctx> (call : CallServiceWithTwoObjects<'service, 'a, 'b, 'c>) (i : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeJSONHandlerWithTwoArg call i
        
    let inline makeJSONHandlerWithTwoArgAsync<'service, 'a, 'b, 'c, 'ctx> (call : CallServiceWithTwoObjectsAsync<'service, 'a, 'b, 'c>) (i : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeJSONHandlerWithTwoArgAsync call i
        
    let inline makeJSONHandlerWithQueryParam<'service, 'a, 'b, 'ctx> (call : CallServiceWithObject<'service, 'a, 'b>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeJSONHandlerWithQueryParam call
        
    let inline makeJSONHandlerWithQueryParamAsync<'service, 'a, 'b, 'ctx> (call : CallServiceWithObjectAsync<'service, 'a, 'b>) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeJSONHandlerWithQueryParamAsync call
        
    let inline makeJSONHandlerWithArgQueryParam<'service, 'a, 'b, 'c, 'ctx> (call : CallServiceWithTwoObjects<'service, 'a, 'b, 'c>) (i : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeJSONHandlerWithArgQueryParam call i
        
    let inline makeJSONHandlerWithArgQueryParamAsync<'service, 'a, 'b, 'c, 'ctx> (call : CallServiceWithTwoObjectsAsync<'service, 'a, 'b, 'c>) (i : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeJSONHandlerWithArgQueryParamAsync call i
        
    let inline makeJSONHandlerWithObjInt<'service, 'a, 'b, 'ctx> (call : CallServiceWithIntAndObject<'service, 'a, 'b>) (i : int) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeJSONHandlerWithObjInt call i
        
    let inline makeJSONHandlerWithObjIntAsync<'service, 'a, 'b, 'ctx> (call : CallServiceWithIntAndObjectAsync<'service, 'a, 'b>) (i : int) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeJSONHandlerWithObjIntAsync call i
        
    let inline makeBinaryPostHandlerWithArgAsync<'service, 'a, 'b, 'ctx> (call : 'service -> 'a -> byte [] -> TaskEither<'b>) (arg : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeBinaryPostHandlerWithArgAsync call arg
        
    let inline makeBinaryResultHandlerWithArgAsync<'service, 'a, 'ctx> (call : 'service -> 'a -> Task<byte []>) (arg : 'a) : HttpHandlerPromise<'ctx> =
        fun builder -> (builder :> Builder.DependencyInjection.ServerBuilder<'ctx>).makeBinaryResultHandlerWithArgAsync call arg