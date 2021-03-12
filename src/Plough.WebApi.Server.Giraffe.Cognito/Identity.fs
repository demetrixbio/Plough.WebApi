namespace Plough.WebApi.Server.Giraffe.Cognito

open System
open System.Security.Claims
open Microsoft.AspNetCore.Http
open Plough.ControlFlow
open Plough.WebApi.Server
open Plough.WebApi.Server.Giraffe.Cognito.Literals

type Person =
    { FirstName : string
      LastName : string
      Email : string
      SubId : string }

type System =
    { ClientId : string option
      ClientName : string option }

type CognitoIdentity =
    | Person of Person
    | System of System

type OfflineIdentity =
    { FirstName : string
      LastName : string
      Email : string
      SubId : string
      ClientId : string
      ClientName : string }


[<AbstractClass>]
type IdentityContext<'identity, 'appIdentity>(ctx : IHttpContextAccessor, isOffline : bool, offlineIdentity : OfflineIdentity) =
    member x.getClaimValue (claimName : string) = ctx.HttpContext.User.FindFirst(claimName).Value

    member x.tryGetClaimValue (claimName : string) =
        if ctx.HttpContext.User.HasClaim(fun s -> s.Type = claimName) then
            x.getClaimValue claimName |> Some
        else
            None
    
    member x.getIdpIdentityFromContext () : TaskEither<CognitoIdentity> =
        let userType = if isOffline then Actor.Person else x.getClaimValue ClaimTypes.Actor |> Enum.Parse<Actor>
        match userType with
        | Actor.Person when not isOffline ->
            { Email = x.getClaimValue ClaimTypes.Email
              SubId = x.getClaimValue ClaimTypes.NameIdentifier
              FirstName = x.getClaimValue ClaimTypes.GivenName
              LastName = x.getClaimValue ClaimTypes.Surname } |> Person |> TaskEither.succeed
            
        | Actor.Person when isOffline ->
            { Email = offlineIdentity.Email
              SubId = offlineIdentity.SubId
              FirstName = offlineIdentity.FirstName
              LastName = offlineIdentity.LastName } |> Person |> TaskEither.succeed
        | Actor.System when not isOffline ->
            { ClientId = x.getClaimValue ClaimTypes.NameIdentifier |> Some
              ClientName = None } |> System |> TaskEither.succeed
        |  Actor.System when isOffline ->
            { ClientId = Some offlineIdentity.ClientId
              ClientName = Some offlineIdentity.ClientName } |> System |> TaskEither.succeed
        | _ ->
            sprintf "Unknown actor type: %A. Cannot decode identity from claims: '%A'." userType ctx.HttpContext.User.Identity
            |> Validation |> TaskEither.fail
    
    abstract member tryGetAppIdentityFromContext : unit -> TaskEither<'appIdentity option>
    abstract member fetchAppIdentity : identity : CognitoIdentity -> TaskEither<'appIdentity>
    abstract member setAppIdentityInContext : identity : 'appIdentity -> TaskEither<unit>
    abstract member mapIdentity : app: 'appIdentity -> idp : CognitoIdentity -> 'identity
    
    interface IIdentityContext<'identity> with

        member x.getIdentity () : TaskEither<'identity> =
            taskEither {
                let! cognitoIdentity = x.getIdpIdentityFromContext ()
                match! x.tryGetAppIdentityFromContext () with
                | Some appIdentity ->
                    return x.mapIdentity appIdentity cognitoIdentity
                | None ->
                    let! appIdentity = x.fetchAppIdentity cognitoIdentity
                    do! x.setAppIdentityInContext appIdentity
                    return x.mapIdentity appIdentity cognitoIdentity
            }