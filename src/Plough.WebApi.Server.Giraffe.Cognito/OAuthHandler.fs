namespace Plough.WebApi.Server.Giraffe.Cognito.OAuth

open System.Text.Json
open Amazon
open Microsoft.AspNetCore.WebUtilities
open Plough.WebApi.Server.Giraffe.Cognito.Literals
      
open System.IdentityModel.Tokens.Jwt
open System.Text
open System.Net.Http.Headers
open System
open System.Collections.Generic
open System.Net.Http
open System.Net
open Amazon.CognitoIdentityProvider
open Amazon.CognitoIdentityProvider.Model
open Amazon.Runtime
open FSharp.Control.Tasks
open System.Security.Claims
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.OAuth
open Microsoft.IdentityModel.Protocols
open Microsoft.IdentityModel.Protocols.OpenIdConnect
open Microsoft.IdentityModel.Tokens
open Newtonsoft.Json

type CognitoOAuthOptions() =
    
    inherit OAuthOptions()
    do
        base.Scope.Add Scope.awsCognitoSigninUserAdmin
        // AWS Cognito claims
        // http://docs.aws.amazon.com/cognito/latest/developerguide/user-pool-settings-attributes.html
        base.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub")
        base.ClaimActions.MapJsonKey(ClaimTypes.StreetAddress, "address")
        base.ClaimActions.MapJsonKey(ClaimTypes.DateOfBirth, "birthdate")
        base.ClaimActions.MapJsonKey(ClaimTypes.Email, "email")
        base.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name")
        base.ClaimActions.MapJsonKey(ClaimTypes.Gender, "gender")
        base.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name")
        base.ClaimActions.MapJsonKey(ClaimTypes.Locality, "locale")
        base.ClaimActions.MapJsonKey("MiddleName", "middle_name")
        base.ClaimActions.MapJsonKey(ClaimTypes.Name, "name")
        base.ClaimActions.MapJsonKey("Nickname", "nickname")
        base.ClaimActions.MapJsonKey(ClaimTypes.HomePhone, "phone_number")
        base.ClaimActions.MapJsonKey("Picture", "picture")
        base.ClaimActions.MapJsonKey("PreferredName", "preferred_username")
        base.ClaimActions.MapJsonKey("Profile", "profile")
        base.ClaimActions.MapJsonKey("Timezone", "timezone")
        base.ClaimActions.MapJsonKey("UpdatedAt", "updated_at")
        base.ClaimActions.MapJsonKey(ClaimTypes.Webpage, "website")
    
    let mutable _userPoolAppDomainPrefix = Unchecked.defaultof<string>
    let mutable _userPoolId = Unchecked.defaultof<string>
    let mutable _resourceServerId = Unchecked.defaultof<string>
    
    /// Amazon Region containing the AWS Cognito User Pool
    member val AmazonRegionEndpoint = RegionEndpoint.USWest2 with get, set
    
    /// Cognito OpenId .well-known configuration endpoint
    member this.MetadataAddress
        with get () = sprintf "https://cognito-idp.%s.amazonaws.com/%s/.well-known/openid-configuration"
                        this.AmazonRegionEndpoint.SystemName this.UserPoolId
                    
    /// Gets the URI where the client will be redirected to authenticate.
    member this.AuthorizationEndpoint
        with get () = base.AuthorizationEndpoint
        and private set value = base.AuthorizationEndpoint <- value
       
    /// Gets the URI the middleware will access to exchange the OAuth token. 
    member this.TokenEndpoint
        with get () = base.TokenEndpoint
        and private set value = base.TokenEndpoint <- value
        
    member this.UserInformationEndpoint
        with get () = base.UserInformationEndpoint
        and private set value = base.UserInformationEndpoint <- value
    
    member private this.BaseUserPoolApplicationDomain
        with get () = sprintf "https://%s.auth.%s.amazoncognito.com/oauth2" this.UserPoolAppDomainPrefix this.AmazonRegionEndpoint.SystemName
    
    member this.UserPoolId
        with get () = _userPoolId
        and set value = _userPoolId <- value
    
    member this.ResourceServerId
        with get () = _resourceServerId
        and set value = _resourceServerId <- value
    
    /// Domain Prefix of the AWS Cognito User Pool Application
    member this.UserPoolAppDomainPrefix
        with get () = _userPoolAppDomainPrefix
        and set value =
            _userPoolAppDomainPrefix <- value
            this.AuthorizationEndpoint <- sprintf "%s/authorize" this.BaseUserPoolApplicationDomain
            this.TokenEndpoint <- sprintf "%s/token" this.BaseUserPoolApplicationDomain
            this.UserInformationEndpoint <- sprintf "%s/userInfo" this.BaseUserPoolApplicationDomain
            
    override this.Validate () =
        base.Validate()
        if String.IsNullOrEmpty(this.UserPoolAppDomainPrefix) then
            failwith "The UserPoolAppDomainPrefix option is required."
        if String.IsNullOrEmpty(this.UserPoolId) then
            failwith "The UserPoolId option is required."
            
type CognitoOAuthHandler (options, logger, encoder, clock) =
    inherit OAuthHandler<CognitoOAuthOptions>(options, logger, encoder, clock)
    
    static let mutable _openIdConfiguration : OpenIdConnectConfiguration option = None
    
    member this.CognitoIdentityProviderClient =
        lazy new AmazonCognitoIdentityProviderClient(AnonymousAWSCredentials(), this.Options.AmazonRegionEndpoint)
    
    //https://developer.okta.com/blog/2018/03/23/token-authentication-aspnetcore-complete-guide#automatic-authorization-server-metadata
    static member GetOpenIdConfigurationAsync(backchannel : HttpClient, metadataAddress : string) =
        async {
            if _openIdConfiguration.IsNone then
                let htmlDocumentRetriever = HttpDocumentRetriever(backchannel)
                let openIdConnectConfigurationRetriever = OpenIdConnectConfigurationRetriever()
                let configurationManager = ConfigurationManager<OpenIdConnectConfiguration>(
                                            metadataAddress,
                                            openIdConnectConfigurationRetriever,
                                            htmlDocumentRetriever)
                let! openIdConfiguration = configurationManager.GetConfigurationAsync() |> Async.AwaitTask
                _openIdConfiguration <- Some openIdConfiguration
            return _openIdConfiguration.Value
        }
    
    override this.CreateTicketAsync(identity, properties, tokens) =
        let ctx = this.Context
        let backChannel = this.Backchannel
        let events = this.Events
        task {
            let request = GetUserRequest(AccessToken = tokens.AccessToken)
            // Get user from AWS Cognito
            let! response = this.CognitoIdentityProviderClient.Value.GetUserAsync(request, ctx.RequestAborted)
            
            if response.HttpStatusCode <> HttpStatusCode.OK then
                return
                    sprintf "An error occurred when retrieving user information. Status code: %A." response.HttpStatusCode
                    |> HttpRequestException
                    |> raise
            else
                let payload =
                    response.UserAttributes
                    |> Seq.map (fun s -> s.Name, s.Value)
                    |> Map.ofSeq
                    |> JsonConvert.SerializeObject
                    |> JsonDocument.Parse
                
                // Get authorized resource server scopes claim from id token
                let! openIdConfig = CognitoOAuthHandler.GetOpenIdConfigurationAsync(backChannel, this.Options.MetadataAddress)
                let tokenValidationParameters =
                    TokenValidationParameters(
                        IssuerSigningKeys=openIdConfig.SigningKeys,
                        ValidateAudience=false,
                        ValidIssuer=openIdConfig.Issuer)
                    
                let claimsIdentity =
                    JwtSecurityTokenHandler()
                        .ValidateToken(tokens.AccessToken, tokenValidationParameters)
                        |> fst
                    
                for scope in claimsIdentity.FindFirst(fun s -> s.Type = JwtClaimType.Scope).Value.Split(" ") do
                    if scope.StartsWith(this.Options.ResourceServerId) then
                        let apiAccessClaim = Claim(JwtClaimType.Scope, scope)
                        identity.AddClaim apiAccessClaim
                        
                identity.AddClaim(Claim(ClaimTypes.Actor, Actor.Person.ToString("g")))
                let clientIdClaim = claimsIdentity.FindFirst(fun s -> s.Type = JwtClaimType.ClientId)
                identity.AddClaim(clientIdClaim)
                
                let context = OAuthCreatingTicketContext(ClaimsPrincipal(identity), properties, ctx, this.Scheme, this.Options, backChannel, tokens, payload.RootElement)
                context.RunClaimActions()
                do! events.CreatingTicket(context)
                return AuthenticationTicket(context.Principal, context.Properties, this.Scheme.Name);
        }
    
    override this.BuildChallengeUrl(properties, redirectUri) =
        // AWS Cognito Authorization Endpoint
        // http://docs.aws.amazon.com/cognito/latest/developerguide/authorization-endpoint.html
        let queryStrings = Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        queryStrings.Add("response_type", "code")
        queryStrings.Add("client_id", this.Options.ClientId)
        queryStrings.Add("redirect_uri", redirectUri)
        //addQueryString queryStrings properties "scope" <| this.FormatScope()
        let state = this.Options.StateDataFormat.Protect(properties)
        queryStrings.Add("state", state)
        QueryHelpers.AddQueryString(this.Options.AuthorizationEndpoint, queryStrings)
        
    override this.ExchangeCodeAsync(context) =
        let ctx = this.Context
        let backChannel = this.Backchannel
        task {
            // AWS Cognito Token Endpoint
            // http://docs.aws.amazon.com/cognito/latest/developerguide/token-endpoint.html
            use requestMessage = new HttpRequestMessage(HttpMethod.Post, this.Options.TokenEndpoint)
            requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))
            let authHeader =
                sprintf "%s:%s" this.Options.ClientId this.Options.ClientSecret
                |> Encoding.UTF8.GetBytes
                |> Convert.ToBase64String

            requestMessage.Headers.Add("Authorization", sprintf "Basic %s" authHeader)
            
            let content = [ ("client_id", this.Options.ClientId)
                            ("redirect_uri", context.RedirectUri)
                            ("code", context.Code)
                            ("grant_type", "authorization_code") ]
                          |> Map.ofSeq
            requestMessage.Content <- new FormUrlEncodedContent(content)
            let! response = backChannel.SendAsync(requestMessage, ctx.RequestAborted)
            let! content = response.Content.ReadAsStringAsync()
            if response.IsSuccessStatusCode then
                return content |> JsonDocument.Parse |> OAuthTokenResponse.Success
            else
                return
                    StringBuilder("OAuth token endpoint failure: ")
                        .AppendFormat("Status: {0};", response.StatusCode)
                        .AppendFormat("Headers: {0};", response.Headers)
                        .AppendFormat("Body: {0};", content)
                        .ToString()
                    |> Exception
                    |> OAuthTokenResponse.Failed
        }