namespace Plough.WebApi.Client

open Plough.WebApi
open Plough.ControlFlow

#if FABLE_COMPILER
open Thoth.Json

type Task<'T> = Async<'T>
type TaskEither<'T> = Async<Either<'T>>
#else
open Thoth.Json.Net
#endif

module Core =
    let ofJson decoder transform json =
        match Decode.fromString decoder json with
        | Ok x -> transform x
        | Error er -> FailureMessage.Validation er |> Error

    let successDecoder decoder =
        Decode.object (fun get ->
            { Data = get.Required.Field "Data" decoder
              Warnings = get.Required.Field "Warnings" (Decode.list Decode.string) })

    let send ofJsonResponse relativeUrl (fetch: string -> Task<Result<'a,string>>): TaskEither<'response> =
        async {
            try
                let res =
                    fetch relativeUrl
                    #if !FABLE_COMPILER
                    |> Async.AwaitTask
                    #endif
                match! res with
                | Ok data -> return ofJsonResponse data
                | Error err ->
                    let problemDecoder = Decode.Auto.generateDecoder<ProblemReport>()
                    return ofJson problemDecoder (ProblemReport.problemReportToFailure >> Either.fail) err
            with
            | e ->
                return sprintf "Call to %s failed. Ex: \n %A" relativeUrl e
                       |> exn |> FailureMessage.ExceptionFailure |> Either.fail
        }
        #if !FABLE_COMPILER
        |> Async.StartAsTask
        #endif
    (* additionalCoders must be public in order to transpile to proper 'export default' statement by fable
       https://github.com/webpack/webpack/issues/4817
       https://github.com/webpack-contrib/imports-loader/issues/68 *)
    let additionalCoders = Extra.empty |> Extra.withInt64 |> Extra.withDecimal
    let inline decoder<'a> = Decode.Auto.generateDecoderCached<'a>(extra = additionalCoders)
    let inline encoder<'a> = Encode.Auto.generateEncoderCached<'a>(extra = additionalCoders)

open Core

// The members of this class must be inlined so Fable can get the generic info
type ApiClient(get, post, getBinary : string -> Task<Result<byte [], string>>, postBinary: byte[] -> string -> Task<Result<string,string>>) =
    
    // we must set raw delegates in order to allow proper inline of api - required for passing generic type info for Fable 
    member x.Raw = {| Get = get; Post = post; GetBinary = getBinary; PostBinary = postBinary |}

    member inline x.Get<'response>(relativeUrl, ?arbitraryType: bool) =
            let ofJson =
                match arbitraryType with
                | Some true -> (decoder<'response>, Either.succeed) ||> ofJson
                | _ -> (successDecoder decoder<'response>, Ok) ||> ofJson
            send ofJson relativeUrl x.Raw.Get

    member inline x.Post<'request, 'response>(relativeUrl, ?payload : 'request) =
            let fetch = payload |> Option.map (encoder<'request> >> Encode.toString 0) |> x.Raw.Post
            let ofJson = (successDecoder decoder<'response>, Ok) ||> ofJson        
            send ofJson relativeUrl fetch
        
    member inline x.GetBinary(relativeUrl:string) : TaskEither<byte []>=
            send Either.succeed relativeUrl x.Raw.GetBinary
        

    /// Send binary to relativeUrl and return JSON response
    member inline x.PostBinary<'response>(relativeUrl, payload : byte []) =
            let fetch = payload  |> x.Raw.PostBinary
            let ofJson = (successDecoder decoder<'response>, Ok) ||> ofJson        
            send ofJson relativeUrl fetch