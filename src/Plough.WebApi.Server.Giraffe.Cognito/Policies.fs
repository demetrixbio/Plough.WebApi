namespace Plough.WebApi.Server.Giraffe.Cognito

open Microsoft.AspNetCore.Authorization
open Microsoft.Extensions.DependencyInjection
open Plough.WebApi.Server.Giraffe.Cognito.Literals

[<AutoOpen>]
module Policies = 
    let addClaimPolicy (options : AuthorizationOptions) resourceServerId (claim : string) =
        options.AddPolicy(claim, fun (builder : AuthorizationPolicyBuilder) ->
            builder.RequireClaim(JwtClaimType.Scope, sprintf "%s/%s" resourceServerId claim)
            |> ignore)
    
    type IServiceCollection with
        member this.AddApiAuthorizationPolicies resourceServerId resourceServerClaims =
            this.AddAuthorization(fun options ->
                resourceServerClaims |> List.iter (addClaimPolicy options resourceServerId))
            |> ignore