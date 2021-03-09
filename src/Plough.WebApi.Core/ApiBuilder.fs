namespace Plough.WebApi

open Plough.WebApi.Server

type ApiServer<'context, 'builder> = ('builder -> HttpHandler<'context>)
type ApiClient<'api> = (ApiClient -> 'api)

type ApiBuilder<'api, 'context, 'builder> =
    abstract member Server : ApiServer<'context, 'builder>
    abstract member Client : ApiClient<'api>