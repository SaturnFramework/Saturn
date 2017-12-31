namespace Saturn

open Microsoft.AspNetCore.Http
open Giraffe.HttpContextExtensions
open Giraffe.Tasks
open Giraffe.HttpHandlers
module Context =

  [<RequireQualifiedAccess>]
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
    let render (ctx: HttpContext) template obj =
      ctx.RenderHtmlAsync (template obj)

    ///Returns to the client static file.
    let file (ctx: HttpContext) path =
      ctx.ReturnHtmlFileAsync path

    ///Gets model from body as JSON.
    let getJson<'a> (ctx: HttpContext) =
      ctx.BindJsonAsync<'a>()

    ///Gets model from body as JSON. Accepts custom serialization settings.
    let getJsonCustom<'a> (ctx : HttpContext) serializer =
      ctx.BindJsonAsync<'a>(serializer)

    ///Gets model from body as JSON.
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

    ///Get model based on `HttpMethod` and `Content-Type` of request. Accepts custom deserialization settings, and culture.
    let getModelCustom<'a> (ctx: HttpContext) serializer culture =
      let clt = culture |> Option.map System.Globalization.CultureInfo.CreateSpecificCulture
      match serializer, clt with
      | Some s, Some c -> ctx.BindModelAsync<'a>(s,c)
      | None, Some c -> ctx.BindModelAsync<'a>(cultureInfo = c)
      | Some s, None -> ctx.BindModelAsync<'a>(settings = s)
      | None, None -> ctx.BindModelAsync<'a>()

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

    let sendDownload (ctx: HttpContext) (path: string) =
      Static.sendFile path (fun c -> task {return Some c}) ctx

    let sendDownloadBinary (ctx: HttpContext) (content: byte []) =
      setBody content (fun c -> task {return Some c}) ctx