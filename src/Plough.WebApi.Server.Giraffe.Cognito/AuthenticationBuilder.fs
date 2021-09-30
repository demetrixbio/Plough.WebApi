namespace Plough.WebApi.Server.Giraffe.Cognito

[<AutoOpen>]
module AuthenticationBuilderExtensions =
    
    open System.Threading.Tasks
    open System.Security.Claims
    open Microsoft.AspNetCore.Authentication.JwtBearer
    open Microsoft.AspNetCore.Authentication;
    open Microsoft.AspNetCore.Http
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.IdentityModel.Tokens
    open Amazon
    open Plough.WebApi.Server.Giraffe.Cognito.Literals
    
    type AuthenticationBuilder with
        member this.AddAwsCognitoOAuth (config : AuthConfig) =
            // https://github.com/leastprivilege/AspNetCoreSecuritySamples/blob/aspnetcore21/OidcAndApi/src/AspNetCoreSecurity/Startup.cs
            // https://github.com/aspnet/Security/issues/1559
            let bindDynamicSchemaSelection (schemaOptions : #AuthenticationSchemeOptions) =
                schemaOptions.ForwardDefaultSelector <- fun ctx -> 
                    match ctx.Request.Headers.TryGetValue "Authorization" with
                    | true, header when header.Count > 0 ->
                        let value = header.[0]
                        if value.StartsWith("Bearer ") then AuthScheme.JwtBearer else null
                    | _ -> null
            
            this
                .AddCookie(AuthScheme.Cookie, fun options ->
                    options.Cookie.Name <- config.IdentityCookieName
                    options.Cookie.Domain <- config.Domain
                    options.LoginPath  <- PathString config.Urls.Login
                    options.LogoutPath <- PathString config.Urls.Logout
                    bindDynamicSchemaSelection options)
                .AddOAuth<OAuth.CognitoOAuthOptions, OAuth.CognitoOAuthHandler>(AuthScheme.OAuth, fun options ->
                    options.ClientId <- config.Cognito.ClientId
                    options.ClientSecret <- config.Cognito.ClientSecret
                    options.CallbackPath <- PathString "/signin-cognito"
                    options.AmazonRegionEndpoint <- RegionEndpoint.GetBySystemName(config.Cognito.Region)
                    options.UserPoolAppDomainPrefix <- config.Cognito.DomainPrefix
                    options.UserPoolId <- config.Cognito.UserPoolId
                    options.ResourceServerId <- config.Cognito.ResourceServerId
                    bindDynamicSchemaSelection options)
            
        member this.AddAwsCognitoJwtBearer(config : AuthConfig) =
            let amazonRegionEndpoint = RegionEndpoint.GetBySystemName(config.Cognito.Region)
            let metadataAddress = sprintf "https://cognito-idp.%s.amazonaws.com/%s/.well-known/openid-configuration"
                                      amazonRegionEndpoint.SystemName config.Cognito.UserPoolId
            
            this.AddJwtBearer(AuthScheme.JwtBearer, fun options ->
                options.MetadataAddress <- metadataAddress
                options.SaveToken <- true
                options.IncludeErrorDetails <- true
                options.RequireHttpsMetadata <- config.Cognito.RequireHttpsMetadata
                options.TokenValidationParameters <-
                    TokenValidationParameters
                        (
                            ValidateIssuerSigningKey = true,
                            ValidateIssuer = true,
                            ValidateLifetime = true,
                            // Do not validate Audience on the "access" token since Cognito does not supply it but it is on the "id_token"
                            ValidateAudience = false
                        )
                options.Events <- JwtBearerEvents(OnTokenValidated = fun context ->
                    let identity = context.Principal.Identity :?> ClaimsIdentity
                    if identity.HasClaim(fun s -> s.Type = JwtClaimType.Scope) then
                        let scopeClaim = identity.FindFirst(JwtClaimType.Scope)
                        identity.RemoveClaim(scopeClaim)
                        scopeClaim.Value.Split(" ")
                        |> Seq.filter (fun s -> s.StartsWith(config.Cognito.ResourceServerId))
                        |> Seq.map (fun apiScope -> Claim(JwtClaimType.Scope, apiScope))
                        |> Seq.iter identity.AddClaim
                    identity.AddClaim(Claim(ClaimTypes.Actor, Actor.System.ToString("g")))
                    Task.CompletedTask))