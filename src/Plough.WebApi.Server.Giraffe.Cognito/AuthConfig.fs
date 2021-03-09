namespace Plough.WebApi.Server.Giraffe.Cognito

[<CLIMutable>]
type AuthUrlConfig =
    { Home : string
      IsLoggedIn : string
      Identity : string
      Claims : string
      Login : string
      Logout : string }

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
      Urls : AuthUrlConfig
      Cognito : CognitoConfig }