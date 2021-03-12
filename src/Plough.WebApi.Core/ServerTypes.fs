namespace Plough.WebApi.Server

open Plough.ControlFlow

/// <summary>
/// A type alias for <see cref="System.Threading.Tasks.Task{HttpContext option}" />  which represents the result of a HTTP function (HttpFunc).
/// If the result is Some HttpContext then the Giraffe middleware will return the response to the client and end the pipeline. However, if the result is None then the Giraffe middleware will continue the ASP.NET Core pipeline by invoking the next middleware.
/// </summary>
type HttpFuncResult<'context> = Task<'context option>

/// <summary>
/// A HTTP function which takes an <see cref="Microsoft.AspNetCore.Http.HttpContext"/> object and returns a <see cref="HttpFuncResult"/>.
/// The function may inspect the incoming <see cref="Microsoft.AspNetCore.Http.HttpRequest"/> and make modifications to the <see cref="Microsoft.AspNetCore.Http.HttpResponse"/> before returning a <see cref="HttpFuncResult"/>. The result can be either a <see cref="System.Threading.Tasks.Task"/> of Some HttpContext or a <see cref="System.Threading.Tasks.Task"/> of None.
/// If the result is Some HttpContext then the Giraffe middleware will return the response to the client and end the pipeline. However, if the result is None then the Giraffe middleware will continue the ASP.NET Core pipeline by invoking the next middleware.
/// </summary>
type HttpFunc<'context> = 'context -> HttpFuncResult<'context>

/// <summary>
/// A HTTP handler is the core building block of a Giraffe web application. It works similarly to ASP.NET Core's middleware where it is self responsible for invoking the next <see cref="HttpFunc"/> function of the pipeline or shortcircuit the execution by directly returning a <see cref="System.Threading.Tasks.Task"/> of HttpContext option.
/// </summary>
type HttpHandler<'context> = HttpFunc<'context> -> HttpFunc<'context>

type FileDownload =
    { Name : string
      Content : byte []
      ContentType : string }

type DownloadWithObject<'a> = 'a -> Either<FileDownload>
type DownloadWithObjectAsync<'a> = 'a -> TaskEither<FileDownload>

type Call<'b> = unit -> Either<'b>
type CallAsync<'b> = unit -> TaskEither<'b>

type CallWithIntAndObject<'a, 'b> = int -> 'a -> Either<'b>
type CallWithIntAndObjectAsync<'a, 'b> = int -> 'a -> TaskEither<'b>

type CallWithObject<'a, 'b> = 'a -> Either<'b>
type CallWithObjectAsync<'a, 'b> = 'a -> TaskEither<'b>

type CallWithTwoObjects<'a, 'b, 'c> = 'a -> 'b -> Either<'c>
type CallWithTwoObjectsAsync<'a, 'b, 'c> = 'a -> 'b -> TaskEither<'c>


type DownloadServiceWithObject<'service, 'a> = 'service -> 'a -> Either<FileDownload>
type DownloadServiceWithObjectAsync<'service, 'a> = 'service -> 'a -> TaskEither<FileDownload>

type CallService<'service, 'b> = 'service -> Either<'b>
type CallServiceAsync<'service, 'b> = 'service -> TaskEither<'b>

type CallServiceWithIntAndObject<'service, 'a, 'b> = 'service -> int -> 'a -> Either<'b>
type CallServiceWithIntAndObjectAsync<'service, 'a, 'b> = 'service -> int -> 'a -> TaskEither<'b>

type CallServiceWithObject<'service, 'a, 'b> = 'service -> 'a -> Either<'b>
type CallServiceWithObjectAsync<'service, 'a, 'b> = 'service -> 'a -> TaskEither<'b>

type CallServiceWithTwoObjects<'service, 'a, 'b, 'c> = 'service -> 'a -> 'b -> Either<'c>
type CallServiceWithTwoObjectsAsync<'service, 'a, 'b, 'c> = 'service -> 'a -> 'b -> TaskEither<'c>