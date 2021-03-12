namespace Plough.WebApi.Server.Giraffe.Cognito

open Plough.WebApi
open Plough.WebApi.Server

[<CLIMutable>]
type CognitoConfig =
    { ClientId : string
      ClientSecret : string
      UserPoolId : string
      DomainPrefix : string
      ResourceServerId : string
      Region : string
      RequireHttpsMetadata : bool }

[<CLIMutable>]
type AuthConfig =
    { IsOffline : bool
      IdentityCookieName : string
      Urls : AuthUrls
      Cognito : CognitoConfig }