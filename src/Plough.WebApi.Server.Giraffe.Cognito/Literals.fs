namespace Plough.WebApi.Server.Giraffe.Cognito.Literals

module AuthScheme =
    open Microsoft.AspNetCore.Authentication.Cookies
    open Microsoft.AspNetCore.Authentication.JwtBearer

    let [<Literal>] Cookie = CookieAuthenticationDefaults.AuthenticationScheme
    let [<Literal>] OAuth = "OAuth"
    let [<Literal>] JwtBearer = JwtBearerDefaults.AuthenticationScheme

module Scope =
    let [<Literal>] phone = "phone"
    let [<Literal>] openId = "openid"
    let [<Literal>] profile = "profile"
    let [<Literal>] email = "email"
    let [<Literal>] awsCognitoSigninUserAdmin  = "aws.cognito.signin.user.admin"

module JwtClaimType =
    /// OpenID Connect requests MUST contain the "openid" scope value. If the openid scope value is not present, the behavior is entirely unspecified.
    /// Other scope values MAY be present.
    /// Scope values used that are not understood by an implementation SHOULD be ignored.
    let [<Literal>] Scope = "scope";
    let [<Literal>] ClientId = "client_id"
    
type Actor = Person = 0 | System = 1