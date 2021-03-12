namespace Plough.WebApi.Server

open Plough.ControlFlow

type IIdentityContext<'identity> =
    abstract member getIdentity : unit -> TaskEither<'identity>
    
[<CLIMutable>]
type AuthUrls =
    { Home : string
      IsLoggedIn : string
      Identity : string
      Claims : string
      Login : string
      Logout : string }