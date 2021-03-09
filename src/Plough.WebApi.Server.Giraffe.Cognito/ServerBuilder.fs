namespace rec Plough.WebApi.Server.Giraffe.Cognito

open FSharp.Control.Tasks
open Giraffe.GiraffeViewEngine
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open Plough.WebApi.Server
open Giraffe.Core
open Giraffe.ResponseWriters
open Giraffe.Auth
open Plough.WebApi.Server.Giraffe.Cognito.Literals

type ServerBuilder<'identity, 'appIdentity, 'idpIdentity>(config : AuthConfig) =
    inherit Giraffe.ServerBuilder()
    
    member x.authFailedHandler : HttpHandler =
        setStatusCode 401 >=> json "Not logged in."
    
    member x.policyFailedHandler policyName =
        let error = sprintf "Not authorized to access %s resources." policyName
        setStatusCode 401 >=> json error
    
    member x.challenge (scheme : string) (redirectUri : string) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            do! ctx.ChallengeAsync(scheme, AuthenticationProperties(RedirectUri = redirectUri))
            return! next ctx |> Async.AwaitTask
        }
        
    member x.signOut (scheme : string) (redirectUri : string) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            do! ctx.SignOutAsync(scheme, AuthenticationProperties(RedirectUri = redirectUri))
            return! next ctx |> Async.AwaitTask
        }
        
    member x.redirectToHome : HttpHandler =
       redirectTo false config.Urls.Home
    
    member x.htmlView (node : XmlNode) : HttpHandler =
        setHttpHeader "Content-Type" "text/html" >=> setBodyFromString (renderHtmlDocument node)

    /// Helper function to turn a sequence of claims (tuples)
    /// into a bare-bones HTML page showing them.
    member x.claimsAsPage (claims : (string * string) seq) =
        html [] [
            head [] [
                title [] [ rawText "Cognito Auth claims view" ]
            ]
            body [] [
                h1 [] [ rawText "User details" ]
                h2 [] [ rawText "Claims:" ]
                ul [] [
                    yield! claims |> Seq.map (
                        fun (key, value) ->
                            li [] [ sprintf "%s: %s" key value |> encodedText ])
                ]
            ]
        ]
    
    interface Auth.ServerBuilder<HttpContext> with
        member x.isOffline : bool =
            config.IsOffline
        
        member x.authenticate : HttpHandler =
            requiresAuthentication x.redirectToHome
        
        member x.authenticateJSON : HttpHandler =
            requiresAuthentication x.authFailedHandler
        
        member x.isLoggedIn : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) ->
            text (if config.IsOffline || ctx.User.Identity.IsAuthenticated then "true" else "false") next ctx
        
        member x.login : HttpHandler =
            x.challenge AuthScheme.OAuth config.Urls.Home
            
        member x.logout : HttpHandler =
            x.signOut AuthScheme.Cookie config.Urls.Home
            
        member x.requirePolicy (policy : string) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                if config.IsOffline then
                    return! next ctx
                else
                    return! authorizeByPolicyName policy (x.policyFailedHandler policy) next ctx
            }

        member x.identity (next : HttpFunc) (ctx : HttpContext) : HttpFuncResult =
            task {
                let identityContext = ctx.GetService<IIdentityContext<'identity, 'appIdentity, 'idpIdentity>>()
                match! identityContext.getIdentity() with
                | Ok identity ->
                    return! json identity.Data next ctx
                | Error failure ->
                    return! x.errorHandler failure next ctx
            }
            
        member x.identityClaims : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) ->
            let claimsAsPage = ctx.User.Claims
                               |> Seq.map (fun c -> (c.Type, c.Value))
                               |> x.claimsAsPage
                               |> htmlView
            claimsAsPage next ctx