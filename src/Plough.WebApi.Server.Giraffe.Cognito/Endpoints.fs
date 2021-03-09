module Plough.WebApi.Server.Giraffe.Cognito.Endpoints

open Plough.WebApi.Server
open Plough.WebApi.Server.Auth

let authenticationEndpoints (config : AuthConfig) = choose [
    // Basic true/false check for logged in status, always accessible
    route config.Urls.IsLoggedIn >=> isLoggedIn
    // Clear the logged-in status for the given session
    route config.Urls.Logout >=> logout
    // The cognito authentication handler.  Usually redirects to a Google site
    // with a bunch of extra metadata, then directs back on success.
    route config.Urls.Login >=> login
    // Get JSON record describing currently logged-in user
    route config.Urls.Identity >=> authenticateJSON >=> identity
    // Debug page that spits out all the claims for the currently authenticated user
    route config.Urls.Claims >=> authenticate >=> identityClaims
]