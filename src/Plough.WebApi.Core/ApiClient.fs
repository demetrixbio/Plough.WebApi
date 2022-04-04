namespace Plough.WebApi.Client

open System
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
    let defaultRequestTimeout = TimeSpan.FromSeconds 100
    
    let private decode decoder json =
        try
            match Decode.fromString decoder json with
            | Ok x ->
                Either.succeed x
            | Error er ->
                Either.fail (FailureMessage.Validation er)
        with
        | _ ->
           Either.fail (FailureMessage.Validation $"Given an invalid json: \n%s{json}")
    
    let ofJson decoder transform json =
        decode decoder json
        |> Either.bind transform

    let successDecoder decoder =
        Decode.object (fun get ->
            { Data = get.Required.Field "Data" decoder
              Warnings = get.Required.Field "Warnings" (Decode.list Decode.string) })
    
    let send ofJsonResponse relativeUrl (timeout : TimeSpan) (fetch: string -> TimeSpan -> Task<Result<'a,string>>): TaskEither<'response> =
        async {
            let res =
                fetch relativeUrl timeout
                #if !FABLE_COMPILER
                |> Async.AwaitTask
                #endif
            match! res with
            | Ok data -> return ofJsonResponse data
            | Error err ->
                let problemDecoder = Decode.Auto.generateDecoder<ProblemReport>()
                return ofJson problemDecoder (ProblemReport.problemReportToFailure >> Either.fail) err
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


type Fetch<'a> = string -> TimeSpan -> Task<Result<'a, string>>


// Members of this class must be inlined so Fable can get the generic info
type ApiClient(get : Fetch<string>,
               post : string option -> Fetch<string>,
               getBinary : Fetch<byte[]>,
               postBinary: byte[] -> Fetch<string>,
               defaultTimeout : TimeSpan option,
               dispose : unit -> unit) =
        
    // we must set raw delegates in order to allow proper inline of api - required for passing generic type info for Fable 
    member x.Raw = {|
        Get = get
        Post = post
        GetBinary = getBinary
        PostBinary = postBinary
        DefaultTimeout = defaultArg defaultTimeout Core.defaultRequestTimeout
    |}
    
    member inline x.Get<'response>(relativeUrl, ?arbitraryType: bool, ?timeout : TimeSpan) =
        let ofJson =
            match arbitraryType with
            | Some true -> (Core.decoder<'response>, Either.succeed) ||> Core.ofJson
            | _ -> (Core.successDecoder Core.decoder<'response>, Ok) ||> Core.ofJson
            
        Core.send ofJson relativeUrl (defaultArg timeout x.Raw.DefaultTimeout) x.Raw.Get

    member inline x.Post<'request, 'response>(relativeUrl, ?payload : 'request, ?timeout : TimeSpan) =
        let fetch = payload |> Option.map (Core.encoder<'request> >> Encode.toString 0) |> x.Raw.Post
        let ofJson = (Core.successDecoder Core.decoder<'response>, Ok) ||> Core.ofJson
        Core.send ofJson relativeUrl (defaultArg timeout x.Raw.DefaultTimeout) fetch
        
    member inline x.GetBinary(relativeUrl:string, ?timeout : TimeSpan) : TaskEither<byte []>=
        Core.send Either.succeed relativeUrl (defaultArg timeout x.Raw.DefaultTimeout) x.Raw.GetBinary
        

    /// Send binary to relativeUrl and return JSON response
    member inline x.PostBinary<'response>(relativeUrl, payload : byte [], ?timeout : TimeSpan) =
        let fetch = payload  |> x.Raw.PostBinary
        let ofJson = (Core.successDecoder Core.decoder<'response>, Ok) ||> Core.ofJson        
        Core.send ofJson relativeUrl (defaultArg timeout x.Raw.DefaultTimeout) fetch
            

    interface IDisposable with
        member this.Dispose() = 
            dispose ()