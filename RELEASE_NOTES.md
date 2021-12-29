## New in 0.1.0 (Released 2021/03/09)
* Release initial WebApi library of Plough

## New in 0.1.1 (Released 2021/03/09)
* Relaxed 3rd party version dependencies

## New in 0.1.2 (Released 2021/03/09)
* Flatten files in projects for Fable compilation

## New in 0.1.3 (Released 2021/03/10)
* net5/netstandard packages for single file publish conflicts

## New in 0.1.4 (Released 2021/03/10)
* Plough.Webapi.[Core|Client.Dotnet|Client.Fable] netstandard packages

## New in 0.2.0 (Released 2021/03/12)
* ServerBuilder / ClientBuilder api settled. Extracted abstract auth builder into core library

## New in 0.2.1 (Released 2021/03/12)
* Fable compatible ClientBuilder api

## New in 0.2.2 (Released 2021/03/13)
* Nuget package conflicts fixed

## New in 0.2.3 (Released 2021/05/05)
* Workaround Fable bug - client builder inheritance from non-primary constructor
* Added support for no auth in dotnet api client
* Added missing server builders for handlers with obj payload

## New in 0.2.4 (Released 2021/05/13)
* Fix injectAuthHeaders sets wrong cookie name

## New in 0.2.5 (Released 2021/06/21)
* Fix indirect references* Added missing server builders for handlers with obj payload

## New in 0.3.1-2 (Released 2021/06/22)
* Plough.ControlFlow compatible release

## New in 0.3.3 (Released 2021/06/22)
* FSharp.Core >= 5.0. Bumped version of Giraffe to 5.0 due to breaking changes

## New in 0.3.4 (Released 2021/06/22)
* FSharp.Core relaxed to 4.7.2 due to single file publish bug. Downgrade version of Giraffe to 4.1 due to breaking changes in 5.0 and dependency on FSharp.Core 5.0.1

## New in 0.4.0 (Released 2021/09/30)
* Add support to share cookie across subdomains

## New in 0.4.1 (Released 2021/12/21)
* Alfonso contrib: Raw delegates properly exposed in ApiClient for inlined GET/POST definitions, needed for Fable to pass type info.

## New in 1.0.0 (Released 2021/12/23)
* F#6 with native task support

## New in 1.0.1 (Released 2021/12/29)
* Brought back Ply task CE due to transaction scope problem with native task CE - https://github.com/dotnet/fsharp/issues/12556