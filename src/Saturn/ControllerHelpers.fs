namespace Saturn

open Microsoft.AspNetCore.Http
open Giraffe.HttpStatusCodeHandlers
open Giraffe.Core
open Giraffe.ResponseWriters
open Giraffe.ModelBinding
open FSharp.Control.Tasks.ContextInsensitive

[<AutoOpen>]
module ControllerHelpers =

  module Controller =

    ///Returns to the client content serialized to JSON.
    let json (ctx: HttpContext) (obj: 'a)   =
      ctx.WriteJsonAsync(obj)

    ///Returns to the client content serialized to JSON. Accepts custom serialization settings
    let jsonCustom (ctx: HttpContext) settings obj=
      ctx.WriteJsonAsync(settings, obj)

    ///Returns to the client content serialized to XML.
    let xml (ctx: HttpContext) (obj: 'a) =
      ctx.WriteXmlAsync obj

    ///Returns to the client content as string.
    let text (ctx: HttpContext) (value: string) =
      ctx.WriteTextAsync value

    ///Returns to the client rendered template.
    let render (ctx: HttpContext) template =
      ctx.WriteHtmlStringAsync template

    ///Returns to the client static file.
    let file (ctx: HttpContext) path =
      ctx.WriteHtmlFileAsync path

    ///Gets model from body as JSON.
    let getJson<'a> (ctx: HttpContext) =
      ctx.BindJsonAsync<'a>()

    ///Gets model from body as XML.
    let getXml<'a> (ctx: HttpContext) =
      ctx.BindXmlAsync<'a>()

    ///Gets model from urelencoded body.
    let getForm<'a> (ctx : HttpContext) =
      ctx.BindFormAsync<'a>()

    ///Gets model from urelencoded body. Accepts culture name
    let getFormCulture<'a> (ctx: HttpContext) culture =
      let clt = System.Globalization.CultureInfo.CreateSpecificCulture culture
      ctx.BindFormAsync<'a> clt

    ///Gets model from query string.
    let getQuery<'a> (ctx : HttpContext) =
      ctx.BindQueryString<'a>()

    ///Gets model from query string. Accepts culture name
    let getQueryCulture<'a> (ctx: HttpContext) culture =
      let clt = System.Globalization.CultureInfo.CreateSpecificCulture culture
      ctx.BindQueryString<'a> clt

    ///Get model based on `HttpMethod` and `Content-Type` of request.
    let getModel<'a> (ctx: HttpContext) =
      match ctx.Items.TryGetValue "RequestModel" with
      | true, o -> task { return unbox<'a> o }
      | _ ->
        ctx.BindModelAsync<'a>()

    ///Get model based on `HttpMethod` and `Content-Type` of request. Accepts custom culture.
    let getModelCustom<'a> (ctx: HttpContext) culture =
      let clt = culture |> Option.map System.Globalization.CultureInfo.CreateSpecificCulture
      match clt with
      | Some c -> ctx.BindModelAsync<'a>(c)
      | None -> ctx.BindModelAsync<'a>()

    ///Loads model populated by `fetchModel` pipeline
    let loadModel<'a> (ctx: HttpContext) =
      match ctx.Items.TryGetValue "RequestModel" with
      | true, o -> Some (unbox<'a> o)
      | _ -> None

    ///Gets path of the request - it's relative to current `scope`
    let getPath (ctx: HttpContext) =
      ctx.Request.Path.Value

    ///Gets url of the request
    let getUrl (ctx: HttpContext) =
      match ctx.Items.TryGetValue "RequestUrl" with
      | true, o -> Some (unbox<string> o)
      | _ -> None

    let getConfig<'a> (ctx: HttpContext) =
      unbox<'a> ctx.Items.["Configuration"]

    let sendDownload (ctx: HttpContext) (path: string) =
      let cnt = System.IO.File.ReadAllBytes path
      setBody cnt  (fun c -> task {return Some c}) ctx

    let sendDownloadBinary (ctx: HttpContext) (content: byte []) =
      setBody content (fun c -> task {return Some c}) ctx

    let redirect (ctx: HttpContext) path =
      redirectTo false path (fun c -> task {return Some c}) ctx

  module Response =
    let continue (ctx: HttpContext) =
      Intermediate.CONTINUE (fun c -> task {return Some c}) ctx

    let switchingProto (ctx: HttpContext) =
      Intermediate.SWITCHING_PROTO (fun c -> task {return Some c}) ctx

    let ok (ctx: HttpContext) res =
      Successful.OK res (fun c -> task {return Some c}) ctx

    let created (ctx: HttpContext) res =
      Successful.CREATED res (fun c -> task {return Some c}) ctx

    let accepted (ctx: HttpContext) res =
      Successful.ACCEPTED res (fun c -> task {return Some c}) ctx

    let badRequest (ctx: HttpContext) res =
      RequestErrors.BAD_REQUEST res (fun c -> task {return Some c}) ctx

    let unauthorized (ctx: HttpContext) scheme relam res =
      RequestErrors.UNAUTHORIZED scheme relam res (fun c -> task {return Some c}) ctx

    let forbidden (ctx: HttpContext) res =
      RequestErrors.FORBIDDEN res (fun c -> task {return Some c}) ctx

    let notFound (ctx: HttpContext) res =
      RequestErrors.NOT_FOUND res (fun c -> task {return Some c}) ctx

    let methodNotAllowed (ctx: HttpContext) res =
      RequestErrors.METHOD_NOT_ALLOWED res (fun c -> task {return Some c}) ctx

    let notAcceptable (ctx: HttpContext) res =
      RequestErrors.NOT_ACCEPTABLE res (fun c -> task {return Some c}) ctx

    let conflict (ctx: HttpContext) res =
      RequestErrors.CONFLICT res (fun c -> task {return Some c}) ctx

    let gone (ctx: HttpContext) res =
      RequestErrors.GONE res (fun c -> task {return Some c}) ctx

    let unuspportedMediaType (ctx: HttpContext) res =
      RequestErrors.UNSUPPORTED_MEDIA_TYPE res (fun c -> task {return Some c}) ctx

    let unprocessableEntity (ctx: HttpContext) res =
      RequestErrors.UNPROCESSABLE_ENTITY res (fun c -> task {return Some c}) ctx

    let preconditionRequired (ctx: HttpContext) res =
      RequestErrors.PRECONDITION_REQUIRED res (fun c -> task {return Some c}) ctx

    let tooManyRequests (ctx: HttpContext) res =
      RequestErrors.TOO_MANY_REQUESTS res (fun c -> task {return Some c}) ctx

    let internalError (ctx: HttpContext) res =
      ServerErrors.INTERNAL_ERROR res (fun c -> task {return Some c}) ctx

    let notImplemented (ctx: HttpContext) res =
      ServerErrors.NOT_IMPLEMENTED res (fun c -> task {return Some c}) ctx

    let badGateway (ctx: HttpContext) res =
      ServerErrors.BAD_GATEWAY res (fun c -> task {return Some c}) ctx

    let serviceUnavailable (ctx: HttpContext) res =
      ServerErrors.SERVICE_UNAVAILABLE res (fun c -> task {return Some c}) ctx

    let gatewayTimeout (ctx: HttpContext) res =
      ServerErrors.GATEWAY_TIMEOUT res (fun c -> task {return Some c}) ctx