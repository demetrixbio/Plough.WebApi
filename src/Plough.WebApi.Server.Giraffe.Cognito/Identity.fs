namespace Plough.WebApi.Server.Giraffe.Cognito

open System
open System.Security.Claims
open Microsoft.AspNetCore.Http
open Plough.ControlFlow
open Plough.WebApi.Server.Giraffe.Cognito.Literals

type PersonalDetails =
    { Email : string
      SubId : string }

type SystemDetails =
    { ClientId : string option
      ClientName : string option }

type IdentityDetails =
    | Person of PersonalDetails
    | System of SystemDetails

type CognitoIdentity =
    { FirstName : string
      LastName : string
      Details : IdentityDetails }

type OfflineIdentity =
    { FirstName : string
      LastName : string
      Email : string
      SubId : string
      ClientId : string
      ClientName : string }

type IIdentityContext<'identity, 'appIdentity, 'idpIdentity> =
    abstract member getClaimValue : claimName : string -> string
    abstract member tryGetClaimValue : claimName : string -> string option
    
    abstract member getIdpIdentityFromContext : unit -> TaskEither<'idpIdentity>
    abstract member tryGetAppIdentityFromContext : unit -> TaskEither<'appIdentity option>
    abstract member fetchAppIdentity : identity : 'idpIdentity -> TaskEither<'appIdentity>
    abstract member setAppIdentityInContext : identity : 'appIdentity -> TaskEither<unit>
    
    abstract member mapIdentity : app: 'appIdentity -> idp : 'idpIdentity -> 'identity
    abstract member getIdentity : unit -> TaskEither<'identity>
    

[<AbstractClass>]
type IdentityContext<'identity, 'appIdentity>(ctx : IHttpContextAccessor, isOffline : bool, offlineIdentity : OfflineIdentity) =
    abstract member tryGetAppIdentityFromContext : unit -> TaskEither<'appIdentity option>
    abstract member fetchAppIdentity : identity : CognitoIdentity -> TaskEither<'appIdentity>
    abstract member setAppIdentityInContext : identity : 'appIdentity -> TaskEither<unit>
    abstract member mapIdentity : app: 'appIdentity -> idp : CognitoIdentity -> 'identity
    
    interface IIdentityContext<'identity, 'appIdentity, CognitoIdentity> with
        member x.getClaimValue (claimName : string) = ctx.HttpContext.User.FindFirst(claimName).Value

        member x.tryGetClaimValue (claimName : string) =
            let service : IIdentityContext<'identity, 'appIdentity, CognitoIdentity> = upcast x
            if ctx.HttpContext.User.HasClaim(fun s -> s.Type = claimName) then
                service.getClaimValue claimName |> Some
            else
                None
        
        member x.getIdpIdentityFromContext () : TaskEither<CognitoIdentity> =
            let service : IIdentityContext<'identity, 'appIdentity, CognitoIdentity> = upcast x
            let userType = if isOffline then Actor.Person else service.getClaimValue ClaimTypes.Actor |> Enum.Parse<Actor>
            match userType with
            | Actor.Person when not isOffline ->
                { Details = { Email = service.getClaimValue ClaimTypes.Email
                              SubId = service.getClaimValue ClaimTypes.NameIdentifier } |> Person
                  FirstName = service.getClaimValue ClaimTypes.GivenName
                  LastName = service.getClaimValue ClaimTypes.Surname } |> TaskEither.succeed
                
            | Actor.Person when isOffline ->
                { Details = { Email = offlineIdentity.Email
                              SubId = offlineIdentity.SubId } |> Person
                  FirstName = offlineIdentity.FirstName
                  LastName = offlineIdentity.LastName } |> TaskEither.succeed
            | Actor.System when not isOffline ->
                { Details = { ClientId = service.getClaimValue ClaimTypes.NameIdentifier |> Some
                              ClientName = None } |> System
                  FirstName = ""
                  LastName = "" } |> TaskEither.succeed
            |  Actor.System when isOffline ->
                { Details = { ClientId = Some offlineIdentity.ClientId
                              ClientName = Some offlineIdentity.ClientName } |> System
                  FirstName = ""
                  LastName = "" } |> TaskEither.succeed
            | _ ->
                sprintf "Unknown actor type: %A. Cannot decode identity from claims: '%A'." userType ctx.HttpContext.User.Identity
                |> Validation |> TaskEither.fail
        
        member x.getIdentity () : TaskEither<'identity> =
            taskEither {
                let service : IIdentityContext<'identity, 'appIdentity, CognitoIdentity> = upcast x
                
                let! cognitoIdentity = service.getIdpIdentityFromContext ()
                match! service.tryGetAppIdentityFromContext () with
                | Some appIdentity ->
                    return service.mapIdentity appIdentity cognitoIdentity
                | None ->
                    let! appIdentity = service.fetchAppIdentity cognitoIdentity
                    do! service.setAppIdentityInContext appIdentity
                    return service.mapIdentity appIdentity cognitoIdentity
            }

        member x.fetchAppIdentity identity = x.fetchAppIdentity identity
        member x.setAppIdentityInContext identity = x.setAppIdentityInContext identity
        member x.tryGetAppIdentityFromContext () = x.tryGetAppIdentityFromContext ()
        member x.mapIdentity app idp = x.mapIdentity app idp