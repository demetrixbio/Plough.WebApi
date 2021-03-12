namespace rec Plough.WebApi.Server.Giraffe

open Plough.ControlFlow
open Plough.WebApi
open Plough.WebApi.Server

open FSharp.Control.Tasks
open System.Globalization
open Microsoft.AspNetCore.Http
open Giraffe.Core
open Giraffe.ModelBinding
open Giraffe.ResponseWriters
open Giraffe.Routing
open Thoth.Json.Net

type ServerBuilder() =
    
    member x.errorHandler failure next ctx  =
        let problem = ProblemReport.failureToProblemReport failure
        let serializedProblem = Encode.Auto.toString(4, problem)
        let errorResponse = clearResponse
                            >=> setStatusCode (ProblemReport.failureTypeAsHttpStatus failure)
                            >=> setHttpHeader "Content-Type" "application/problem+json" // https://tools.ietf.org/html/rfc7807
                            >=> setBodyFromString serializedProblem
        errorResponse next ctx

    member x.fileDownloadToStatusCode (fileDownload : Either<FileDownload>) =
        match fileDownload with
        | Ok { Data = result; Warnings = _ } ->
            setHttpHeader "Content-Disposition" (sprintf "attachment; filename=%s" result.Name)
            >=> setHttpHeader "Content-Type" result.ContentType
            >=> setBody result.Content
        | Error failure -> x.errorHandler failure    

    /// Map internal failure types onto http codes and set status
    member x.resultToHttpStatusCode response next ctx  =
        match response with
        | Ok success -> (json success) next ctx
        | Error failure -> x.errorHandler failure next ctx
    
    
    // ---------------------------
    // Auth handling functions
    // ---------------------------
    abstract isOffline : bool
    default x.isOffline = false
    
    abstract isLoggedIn : HttpHandler<HttpContext>
    default x.isLoggedIn = failwith "Auth not configured"
    
    abstract authenticate : HttpHandler<HttpContext>
    default x.authenticate = failwith "Auth not configured"

    abstract authenticateJSON : HttpHandler<HttpContext>
    default x.authenticateJSON = failwith "Auth not configured"
    
    abstract login : HttpHandler<HttpContext>
    default x.login = failwith "Auth not configured"
    
    abstract logout : HttpHandler<HttpContext>
    default x.logout = failwith "Auth not configured"
    
    abstract requirePolicy : policy : string -> HttpHandler<HttpContext>
    default x.requirePolicy _policy = failwith "Auth not configured"
    
    abstract identityClaims : HttpHandler<HttpContext>
    default x.identityClaims = failwith "Auth not configured"
    
    abstract identity : next : HttpFunc<HttpContext> -> ctx : HttpContext -> HttpFuncResult<HttpContext>
    default x.identity _next _ctx = failwith "Auth not configured"
    
    interface ServerBuilder<HttpContext> with
        // ---------------------------
        // Auth handling functions
        // ---------------------------
        member x.isOffline = x.isOffline
        member x.isLoggedIn = x.isLoggedIn
        member x.authenticate = x.authenticate
        member x.authenticateJSON = x.authenticateJSON
        member x.login = x.login
        member x.logout = x.logout
        member x.requirePolicy policy = x.requirePolicy policy
        member x.identityClaims = x.identityClaims
        member x.identity next ctx = x.identity next ctx
        
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
        member x.warbler (f : (HttpFunc * HttpContext) -> HttpFunc -> HttpContext -> 'd) (next : HttpFunc) (ctx : HttpContext) : 'd =
            warbler f next ctx
        
        /// <summary>
        /// Use skipPipeline to shortcircuit the <see cref="HttpHandler"/> pipeline and return None to the surrounding <see cref="HttpHandler"/> or the Giraffe/Suave middleware (which would subsequently invoke the next middleware as a result of it).
        /// </summary>
        member x.skipPipeline : HttpFuncResult =
            skipPipeline
        
        /// <summary>
        /// Use earlyReturn to shortcircuit the <see cref="HttpHandler"/> pipeline and return Some HttpContext to the surrounding <see cref="HttpHandler"/> or the Giraffe/Suave middleware (which would subsequently end the pipeline by returning the response back to the client).
        /// </summary>
        member x.earlyReturn : HttpFunc =
            earlyReturn
        
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
        member x.handleContext (contextMap : HttpContext -> HttpFuncResult) (next : HttpFunc) (ctx : HttpContext) : HttpFuncResult =
            handleContext contextMap next ctx
        
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
        member x.compose (handler1 : HttpHandler) (handler2 : HttpHandler) (final : HttpFunc) : HttpFunc =
            compose handler1 handler2 final
        
        /// <summary>
        /// Iterates through a list of <see cref="HttpHandler"/> functions and returns the result of the first <see cref="HttpHandler"/> of which the outcome is Some HttpContext.
        /// Please mind that all <see cref="HttpHandler"/> functions will get pre-evaluated at runtime by applying the next (HttpFunc) parameter to each handler.
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="next"></param>
        /// <returns>A <see cref="HttpFunc"/>.</returns>
        member x.choose (handlers : HttpHandler list) (next : HttpFunc) : HttpFunc =
            choose handlers next
        
        // ---------------------------
        // Default HttpHandlers
        // ---------------------------
        
        member x.GET      : HttpHandler = GET
        member x.POST     : HttpHandler = POST
        member x.PUT      : HttpHandler = PUT
        member x.PATCH    : HttpHandler = PATCH
        member x.DELETE   : HttpHandler = DELETE
        member x.HEAD     : HttpHandler = HEAD
        member x.OPTIONS  : HttpHandler = OPTIONS
        member x.TRACE    : HttpHandler = TRACE
        member x.CONNECT  : HttpHandler = CONNECT
        member x.GET_HEAD : HttpHandler = GET_HEAD
        
        /// <summary>
        /// Clears the current <see cref="Microsoft.AspNetCore.Http.HttpResponse"/> object.
        /// This can be useful if a <see cref="HttpHandler"/> function needs to overwrite the response of all previous <see cref="HttpHandler"/> functions with its own response (most commonly used by an <see cref="ErrorHandler"/> function).
        /// </summary>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.clearResponse : HttpHandler =
            clearResponse
            
        /// <summary>
        /// Sets the HTTP status code of the response.
        /// </summary>
        /// <param name="statusCode">The status code to be set in the response. For convenience you can use the static <see cref="Microsoft.AspNetCore.Http.StatusCodes"/> class for passing in named status codes instead of using pure int values.</param>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.setStatusCode (statusCode : int) : HttpHandler =
            setStatusCode statusCode
            
        /// <summary>
        /// Adds or sets a HTTP header in the response.
        /// </summary>
        /// <param name="key">The HTTP header name. For convenience you can use the static <see cref="Microsoft.Net.Http.Headers.HeaderNames"/> class for passing in strongly typed header names instead of using pure string values.</param>
        /// <param name="value">The value to be set. Non string values will be converted to a string using the object's ToString() method.</param>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.setHttpHeader (key : string) (value : obj) : HttpHandler =
            setHttpHeader key value
            
        /// <summary>
        /// Filters an incoming HTTP request based on the accepted mime types of the client (Accept HTTP header).
        /// If the client doesn't accept any of the provided mimeTypes then the handler will not continue executing the next <see cref="HttpHandler"/> function.
        /// </summary>
        /// <param name="mimeTypes">List of mime types of which the client has to accept at least one.</param>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.mustAccept (mimeTypes : string list) : HttpHandler =
            mustAccept mimeTypes
            
        /// <summary>
        /// Redirects to a different location with a `302` or `301` (when permanent) HTTP status code.
        /// </summary>
        /// <param name="permanent">If true the redirect is permanent (301), otherwise temporary (302).</param>
        /// <param name="location">The URL to redirect the client to.</param>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.redirectTo (permanent : bool) (location : string) : HttpHandler =
            redirectTo permanent location
            
        // ---------------------------
        // Model binding functions
        // ---------------------------

        /// <summary>
        /// Parses a JSON payload into an instance of type 'T.
        /// </summary>
        /// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
        /// <typeparam name="'T"></typeparam>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.bindJson<'T> (f : 'T -> HttpHandler) : HttpHandler =
            bindJson f
            
        /// <summary>
        /// Parses a XML payload into an instance of type 'T.
        /// </summary>
        /// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
        /// <typeparam name="'T"></typeparam>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.bindXml<'T> (f : 'T -> HttpHandler) : HttpHandler =
            bindXml f
            
        /// <summary>
        /// Parses a HTTP form payload into an instance of type 'T.
        /// </summary>
        /// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
        /// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
        /// <typeparam name="'T"></typeparam>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.bindForm<'T> (culture : CultureInfo option) (f : 'T -> HttpHandler) : HttpHandler =
            bindForm culture f
            
        /// <summary>
        /// Tries to parse a HTTP form payload into an instance of type 'T.
        /// </summary>
        /// <param name="parsingErrorHandler">A <see cref="System.String"/> -> <see cref="HttpHandler"/> function which will get invoked when the model parsing fails. The <see cref="System.String"/> parameter holds the parsing error message.</param>
        /// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
        /// <param name="successHandler">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
        /// <typeparam name="'T"></typeparam>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.tryBindForm<'T> (parsingErrorHandler : string -> HttpHandler) (culture : CultureInfo option) (successHandler : 'T -> HttpHandler) : HttpHandler =
            tryBindForm parsingErrorHandler culture successHandler
            
        /// <summary>
        /// Parses a HTTP query string into an instance of type 'T.
        /// </summary>
        /// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
        /// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
        /// <typeparam name="'T"></typeparam>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.bindQuery<'T> (culture : CultureInfo option) (f : 'T -> HttpHandler) : HttpHandler =
            bindQuery culture f
            
        /// <summary>
        /// Tries to parse a query string into an instance of type `'T`.
        /// </summary>
        /// <param name="parsingErrorHandler">A <see href="HttpHandler"/> function which will get invoked when the model parsing fails. The <see cref="System.String"/> input parameter holds the parsing error message.</param>
        /// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
        /// <param name="successHandler">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
        /// <typeparam name="'T"></typeparam>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.tryBindQuery<'T> (parsingErrorHandler : string -> HttpHandler) (culture : CultureInfo option) (successHandler : 'T -> HttpHandler) : HttpHandler =
            tryBindQuery parsingErrorHandler culture successHandler
            
        /// <summary>
        /// Parses a HTTP payload into an instance of type 'T.
        /// The model can be sent via XML, JSON, form or query string.
        /// </summary>
        /// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
        /// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
        /// <typeparam name="'T"></typeparam>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.bindModel<'T> (culture : CultureInfo option) (f : 'T -> HttpHandler) : HttpHandler =
            bindModel culture f
            
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
        member x.setBody (bytes : byte array) : HttpHandler =
            setBody bytes
            
        /// <summary>
        /// Writes an UTF-8 encoded string to the body of the HTTP response and sets the HTTP Content-Length header accordingly.
        /// </summary>
        /// <param name="str">The string value to be send back to the client.</param>
        /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
        member x.setBodyFromString (str : string) : HttpHandler =
            setBodyFromString str
            
        /// <summary>
        /// Writes an UTF-8 encoded string to the body of the HTTP response and sets the HTTP Content-Length header accordingly, as well as the Content-Type header to text/plain.
        /// </summary>
        /// <param name="str">The string value to be send back to the client.</param>
        /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
        member x.text (str : string) : HttpHandler =
            text str
        
        /// <summary>
        /// Serializes an object to JSON and writes the output to the body of the HTTP response.
        /// It also sets the HTTP Content-Type header to application/json and sets the Content-Length header accordingly.
        /// The JSON serializer can be configured in the ASP.NET Core startup code by registering a custom class of type <see cref="Json.ISerializer"/>.
        /// </summary>
        /// <param name="dataObj">The object to be send back to the client.</param>
        /// <typeparam name="'T"></typeparam>
        /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
        member x.json<'T> (dataObj : 'T) : HttpHandler =
            json dataObj
        
        /// <summary>
        /// Serializes an object to JSON and writes the output to the body of the HTTP response using chunked transfer encoding.
        /// It also sets the HTTP Content-Type header to application/json and sets the Transfer-Encoding header to chunked.
        /// The JSON serializer can be configured in the ASP.NET Core startup code by registering a custom class of type <see cref="Json.ISerializer"/>.
        /// </summary>
        /// <param name="dataObj">The object to be send back to the client.</param>
        /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
        member x.jsonChunked<'T> (dataObj : 'T) : HttpHandler =
            jsonChunked dataObj
        
        /// <summary>
        /// Serializes an object to XML and writes the output to the body of the HTTP response.
        /// It also sets the HTTP Content-Type header to application/xml and sets the Content-Length header accordingly.
        /// The JSON serializer can be configured in the ASP.NET Core startup code by registering a custom class of type <see cref="Xml.ISerializer"/>.
        /// </summary>
        /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
        member x.xml (dataObj : obj) : HttpHandler =
            xml dataObj
        
        /// <summary>
        /// Reads a HTML file from disk and writes its contents to the body of the HTTP response.
        /// It also sets the HTTP header Content-Type to text/html and sets the Content-Length header accordingly.
        /// </summary>
        /// <param name="filePath">A relative or absolute file path to the HTML file.</param>
        /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
        member x.htmlFile (filePath : string) : HttpHandler =
            htmlFile filePath
        
        /// <summary>
        /// Writes a HTML string to the body of the HTTP response.
        /// It also sets the HTTP header Content-Type to text/html and sets the Content-Length header accordingly.
        /// </summary>
        /// <param name="html">The HTML string to be send back to the client.</param>
        /// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
        member x.htmlString (html : string) : HttpHandler =
            htmlString html
            
        // ---------------------------
        // Routing functions
        // ---------------------------
        
        /// <summary>
        /// Filters an incoming HTTP request based on the port.
        /// </summary>
        /// <param name="fns">List of port to <see cref="HttpHandler"/> mappings</param>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.routePorts (fns : (int * HttpHandler) list) : HttpHandler =
            routePorts fns
            
        /// <summary>
        /// Filters an incoming HTTP request based on the request path (case sensitive).
        /// </summary>
        /// <param name="path">Request path.</param>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.route (path : string) : HttpHandler =
            route path
            
        /// <summary>
        /// Filters an incoming HTTP request based on the request path (case insensitive).
        /// </summary>
        /// <param name="path">Request path.</param>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.routeCi (path : string) : HttpHandler =
            routeCi path
        
        /// <summary>
        /// Filters an incoming HTTP request based on the request path using Regex (case sensitive).
        /// </summary>
        /// <param name="path">Regex path.</param>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.routex (path : string) : HttpHandler =
            routex path
        
        /// <summary>
        /// Filters an incoming HTTP request based on the request path using Regex (case insensitive).
        /// </summary>
        /// <param name="path">Regex path.</param>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.routeCix (path : string) : HttpHandler =
            routeCix path
            
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
        member x.routef (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandler) : HttpHandler =
            routef path routeHandler
            
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
        member x.routeCif (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandler) : HttpHandler =
            routeCif path routeHandler
            
        /// <summary>
        /// Filters an incoming HTTP request based on the request path (case insensitive).
        /// If the route matches the incoming HTTP request then the parameters from the string will be used to create an instance of 'T and subsequently passed into the supplied routeHandler.
        /// </summary>
        /// <param name="route">A string representing the expected request path. Use {propertyName} for reserved parameter names which should map to the properties of type 'T. You can also use valid Regex within the route string.</param>
        /// <param name="routeHandler">A function which accepts a tuple 'T of the parsed parameters and returns a <see cref="HttpHandler"/> function which will subsequently deal with the request.</param>
        /// <typeparam name="'T"></typeparam>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.routeBind<'T> (route : string) (routeHandler : 'T -> HttpHandler) : HttpHandler =
            routeBind route routeHandler
            
        /// <summary>
        /// Filters an incoming HTTP request based on the beginning of the request path (case sensitive).
        /// </summary>
        /// <param name="subPath">The expected beginning of a request path.</param>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.routeStartsWith (subPath : string) : HttpHandler =
            routeStartsWith subPath
            
        /// <summary>
        /// Filters an incoming HTTP request based on the beginning of the request path (case insensitive).
        /// </summary>
        /// <param name="subPath">The expected beginning of a request path.</param>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.routeStartsWithCi (subPath : string) : HttpHandler =
            routeStartsWithCi subPath
            
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
        member x.routeStartsWithf (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandler) : HttpHandler =
            routeStartsWithf path routeHandler
            
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
        member x.routeStartsWithCif (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandler) : HttpHandler =
            routeStartsWithCif path routeHandler
            
        /// <summary>
        /// Filters an incoming HTTP request based on a part of the request path (case sensitive).
        /// Subsequent route handlers inside the given handler function should omit the already validated path.
        /// </summary>
        /// <param name="path">A part of an expected request path.</param>
        /// <param name="handler">A Giraffe <see cref="HttpHandler"/> function.</param>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.subRoute (path : string) (handler : HttpHandler) : HttpHandler =
            subRoute path handler
            
        /// <summary>
        /// Filters an incoming HTTP request based on a part of the request path (case insensitive).
        /// Subsequent route handlers inside the given handler function should omit the already validated path.
        /// </summary>
        /// <param name="path">A part of an expected request path.</param>
        /// <param name="handler">A Giraffe <see cref="HttpHandler"/> function.</param>
        /// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
        member x.subRouteCi (path : string) (handler : HttpHandler) : HttpHandler =
            subRouteCi path handler
            
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
        member x.subRoutef (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandler) : HttpHandler =
            subRoutef path routeHandler
            

        // ---------------------------
        // Plain (no dependency injection) route handling functions
        // ---------------------------
        member x.makeDownloadHandlerWithArg<'a> (download : DownloadWithObject<'a>) (param : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let response = download param
                    return! x.fileDownloadToStatusCode response next ctx
                }
        
        member x.makeDownloadHandlerWithArgAsync<'a> (download : DownloadWithObjectAsync<'a>) (param : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let! response = param |> download
                    return! x.fileDownloadToStatusCode response next ctx
                }
                            
        member x.makeDownloadHandlerNoArg (download : DownloadWithObject<unit>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let response = download()
                    return! x.fileDownloadToStatusCode response next ctx
                }
            
        member x.makeDownloadHandlerNoArgAsync (download : DownloadWithObjectAsync<unit>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let! response = download ()
                    return! x.fileDownloadToStatusCode response next ctx
                }
         
        member x.makeDownloadHandlerWithObj<'a> (download : DownloadWithObject<'a>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {       
                    let! input = ctx.BindJsonAsync<'a>()
                    let response = download input
                    return! x.fileDownloadToStatusCode response next ctx
                }
            
        member x.makeDownloadHandlerWithObjAsync<'a> (download : DownloadWithObjectAsync<'a>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {       
                    let! input = ctx.BindJsonAsync<'a>()
                    let! response = download input
                    return! x.fileDownloadToStatusCode response next ctx
                }
        
        member x.makeJSONHandler<'b> (call : Call<'b>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let response = call ()
                    return! x.resultToHttpStatusCode response next ctx
                }
                
        member x.makeJSONHandlerAsync<'b> (call : CallAsync<'b>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let! response = call ()
                    return! x.resultToHttpStatusCode response next ctx
                }
                
        member x.makeJSONHandlerWithArg<'a, 'b> (call : CallWithObject<'a, 'b>) (i : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let response = call i
                    return! x.resultToHttpStatusCode response next ctx
                }
        
        member x.makeJSONHandlerWithArgAsync<'a, 'b> (call : CallWithObjectAsync<'a, 'b>) (i : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let! response = call i
                    return! x.resultToHttpStatusCode response next ctx
                }
                
        member x.makeJSONHandlerWithTwoArg<'a, 'b, 'c> (call : CallWithTwoObjects<'a, 'b, 'c>) (i : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let! input = ctx.BindJsonAsync<'b>()
                    let response = call i input
                    return! x.resultToHttpStatusCode response next ctx
                }
        
        member x.makeJSONHandlerWithTwoArgAsync<'a, 'b, 'c> (call : CallWithTwoObjectsAsync<'a, 'b, 'c>) (i : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let! input = ctx.BindJsonAsync<'b>()
                    let! response = call i input
                    return! x.resultToHttpStatusCode response next ctx
                }
                
        member x.makeJSONHandlerWithQueryParam<'a, 'b> (call : CallWithObject<'a, 'b>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let queryParams = ctx.BindQueryString<'a>()
                    let response = call queryParams
                    return! x.resultToHttpStatusCode response next ctx
                }
            
        member x.makeJSONHandlerWithQueryParamAsync<'a, 'b> (call : CallWithObjectAsync<'a, 'b>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let queryParams = ctx.BindQueryString<'a>()
                    let! response = call queryParams
                    return! x.resultToHttpStatusCode response next ctx
                }
                
        member x.makeJSONHandlerWithArgQueryParam<'a, 'b, 'c> (call : CallWithTwoObjects<'a, 'b, 'c>) (i : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let queryParams = ctx.BindQueryString<'b>()
                    let response = call i queryParams
                    return! x.resultToHttpStatusCode response next ctx
                }
            
        member x.makeJSONHandlerWithArgQueryParamAsync<'a, 'b, 'c> (call : CallWithTwoObjectsAsync<'a, 'b, 'c>) (i : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let queryParams = ctx.BindQueryString<'b>()
                    let! response = call i queryParams
                    return! x.resultToHttpStatusCode response next ctx
                }
                
        member x.makeJSONHandlerWithObj<'a, 'b> (call : CallWithObject<'a, 'b>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let! input = ctx.BindJsonAsync<'a>()
                    let response = call input
                    return! x.resultToHttpStatusCode response next ctx
                }
            
        member x.makeJSONHandlerWithObjAsync<'a, 'b> (call : CallWithObjectAsync<'a, 'b>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let! input = ctx.BindJsonAsync<'a>()
                    let! response = call input
                    return! x.resultToHttpStatusCode response next ctx
                }
        
        member x.makeJSONHandlerWithObjInt<'a, 'b> (call : CallWithIntAndObject<'a, 'b>) (i : int) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let! input = ctx.BindJsonAsync<'a>()
                    let response = call i input
                    return! x.resultToHttpStatusCode response next ctx
                }
            
        member x.makeJSONHandlerWithObjIntAsync<'a, 'b> (call : CallWithIntAndObjectAsync<'a, 'b>) (i : int) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let! input = ctx.BindJsonAsync<'a>()
                    let! response = call i input
                    return! x.resultToHttpStatusCode response next ctx
                }
        
        member x.makeBinaryPostHandlerWithArgAsync<'a, 'b> (call : 'a -> byte [] -> TaskEither<'b>) (arg : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    use ms = new System.IO.MemoryStream()
                    do! ctx.Request.Body.CopyToAsync(ms)
                    let input = ms.ToArray()
                    
                    let! response = call arg input
                    return! x.resultToHttpStatusCode response next ctx
                }
            
        member x.makeBinaryResultHandlerWithArgAsync<'a, 'b> (call : 'a -> Task<byte []>) (arg : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let! bytes = call arg
                    do! ctx.Response.Body.WriteAsync(bytes,0,bytes.Length)
                    return! next ctx
               }

        // ---------------------------
        // Dependency injection route handling functions
        // ---------------------------
        member x.makeDownloadHandlerWithArg<'service, 'a> (download : DownloadServiceWithObject<'service, 'a>) (param : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let response = download service param
                    return! x.fileDownloadToStatusCode response next ctx
                }
        
        member x.makeDownloadHandlerWithArgAsync<'service, 'a> (download : DownloadServiceWithObjectAsync<'service, 'a>) (param : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let! response = param |> download service
                    return! x.fileDownloadToStatusCode response next ctx
                }
                            
        member x.makeDownloadHandlerNoArg<'service> (download : DownloadServiceWithObject<'service, unit>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let response = download service ()
                    return! x.fileDownloadToStatusCode response next ctx
                }
            
        member x.makeDownloadHandlerNoArgAsync<'service> (download : DownloadServiceWithObjectAsync<'service, unit>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let! response = download service ()
                    return! x.fileDownloadToStatusCode response next ctx
                }
         
        member x.makeDownloadHandlerWithObj<'service, 'a> (download : DownloadServiceWithObject<'service, 'a>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let! input = ctx.BindJsonAsync<'a>()
                    let response = download service input
                    return! x.fileDownloadToStatusCode response next ctx
                }
            
        member x.makeDownloadHandlerWithObjAsync<'service, 'a> (download : DownloadServiceWithObjectAsync<'service, 'a>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let! input = ctx.BindJsonAsync<'a>()
                    let! response = download service input
                    return! x.fileDownloadToStatusCode response next ctx
                }
        
        member x.makeJSONHandler<'service, 'b> (call : CallService<'service, 'b>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let response = call service
                    return! x.resultToHttpStatusCode response next ctx
                }
                
        member x.makeJSONHandlerAsync<'service, 'b> (call : CallServiceAsync<'service, 'b>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let! response = call service
                    return! x.resultToHttpStatusCode response next ctx
                }
                
        member x.makeJSONHandlerWithArg<'service, 'a, 'b> (call : CallServiceWithObject<'service, 'a, 'b>) (i : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let response = call service i
                    return! x.resultToHttpStatusCode response next ctx
                }
        
        member x.makeJSONHandlerWithArgAsync<'service, 'a, 'b> (call : CallServiceWithObjectAsync<'service, 'a, 'b>) (i : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let! response = call service i
                    return! x.resultToHttpStatusCode response next ctx
                }
                
        member x.makeJSONHandlerWithTwoArg<'service, 'a, 'b, 'c> (call : CallServiceWithTwoObjects<'service, 'a, 'b, 'c>) (i : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let! input = ctx.BindJsonAsync<'b>()
                    let response = call service i input
                    return! x.resultToHttpStatusCode response next ctx
                }
        
        member x.makeJSONHandlerWithTwoArgAsync<'service, 'a, 'b, 'c> (call : CallServiceWithTwoObjectsAsync<'service, 'a, 'b, 'c>) (i : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let! input = ctx.BindJsonAsync<'b>()
                    let! response = call service i input
                    return! x.resultToHttpStatusCode response next ctx
                }
                
        member x.makeJSONHandlerWithQueryParam<'service, 'a, 'b> (call : CallServiceWithObject<'service, 'a, 'b>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let queryParams = ctx.BindQueryString<'a>()
                    let response = call service queryParams
                    return! x.resultToHttpStatusCode response next ctx
                }
            
        member x.makeJSONHandlerWithQueryParamAsync<'service, 'a, 'b> (call : CallServiceWithObjectAsync<'service, 'a, 'b>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let queryParams = ctx.BindQueryString<'a>()
                    let! response = call service queryParams
                    return! x.resultToHttpStatusCode response next ctx
                }
                
        member x.makeJSONHandlerWithArgQueryParam<'service, 'a, 'b, 'c> (call : CallServiceWithTwoObjects<'service, 'a, 'b, 'c>) (i : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let queryParams = ctx.BindQueryString<'b>()
                    let response = call service i queryParams
                    return! x.resultToHttpStatusCode response next ctx
                }
            
        member x.makeJSONHandlerWithArgQueryParamAsync<'service, 'a, 'b, 'c> (call : CallServiceWithTwoObjectsAsync<'service, 'a, 'b, 'c>) (i : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let queryParams = ctx.BindQueryString<'b>()
                    let! response = call service i queryParams
                    return! x.resultToHttpStatusCode response next ctx
                }
                
        member x.makeJSONHandlerWithObj<'service, 'a, 'b> (call : CallServiceWithObject<'service, 'a, 'b>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let! input = ctx.BindJsonAsync<'a>()
                    let response = call service input
                    return! x.resultToHttpStatusCode response next ctx
                }
            
        member x.makeJSONHandlerWithObjAsync<'service, 'a, 'b> (call : CallServiceWithObjectAsync<'service, 'a, 'b>) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let! input = ctx.BindJsonAsync<'a>()
                    let! response = call service input
                    return! x.resultToHttpStatusCode response next ctx
                }
        
        member x.makeJSONHandlerWithObjInt<'service, 'a, 'b> (call : CallServiceWithIntAndObject<'service, 'a, 'b>) (i : int) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let! input = ctx.BindJsonAsync<'a>()
                    let response = call service i input
                    return! x.resultToHttpStatusCode response next ctx
                }
            
        member x.makeJSONHandlerWithObjIntAsync<'service, 'a, 'b> (call : CallServiceWithIntAndObjectAsync<'service, 'a, 'b>) (i : int) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let! input = ctx.BindJsonAsync<'a>()
                    let! response = call service i input
                    return! x.resultToHttpStatusCode response next ctx
                }
        
        member x.makeBinaryPostHandlerWithArgAsync<'service, 'a, 'b> (call : 'service -> 'a -> byte [] -> TaskEither<'b>) (arg : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    use ms = new System.IO.MemoryStream()
                    do! ctx.Request.Body.CopyToAsync(ms)
                    let input = ms.ToArray()
                    
                    let! response = call service arg input
                    return! x.resultToHttpStatusCode response next ctx
                }
            
        member x.makeBinaryResultHandlerWithArgAsync<'service, 'a, 'b> (call : 'service -> 'a -> Task<byte []>) (arg : 'a) : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let service = ctx.GetService<'service>()
                    let! bytes = call service arg
                    do! ctx.Response.Body.WriteAsync(bytes,0,bytes.Length)
                    return! next ctx
               }