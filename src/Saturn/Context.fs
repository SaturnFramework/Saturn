namespace Saturn

open Microsoft.AspNetCore.Http
open Giraffe.HttpContextExtensions
open Giraffe.Tasks
module Context =

  [<RequireQualifiedAccess>]
  module Controler =

    let json (ctx: HttpContext) (obj: 'a)   =
      ctx.WriteJsonAsync(obj)

    let jsonCustom (ctx: HttpContext) settings obj=
      ctx.WriteJsonAsync(settings, obj)

    let xml (ctx: HttpContext) (obj: 'a) =
      ctx.WriteXmlAsync obj

    let text (ctx: HttpContext) (value: string) =
      ctx.WriteTextAsync value

    let render (ctx: HttpContext) template obj =
      ctx.RenderHtmlAsync (template obj)

    let file (ctx: HttpContext) path =
      ctx.ReturnHtmlFileAsync path //TODO: create better file plug

    let getJson<'a> (ctx: HttpContext) =
      ctx.BindJsonAsync<'a>()

    let getJsonCustom<'a> (ctx : HttpContext) serializer =
      ctx.BindJsonAsync<'a>(serializer)

    let getForm<'a> (ctx : HttpContext) =
      ctx.BindFormAsync<'a>()

    let getFormCulture<'a> (ctx: HttpContext) culture =
      let clt = System.Globalization.CultureInfo.CreateSpecificCulture culture
      ctx.BindFormAsync<'a> clt

    let getQuery<'a> (ctx : HttpContext) =
      ctx.BindQueryString<'a>()

    let getQueryCulture<'a> (ctx: HttpContext) culture =
      let clt = System.Globalization.CultureInfo.CreateSpecificCulture culture
      ctx.BindQueryString<'a> clt

    let getModel<'a> (ctx: HttpContext) =
      match ctx.Items.TryGetValue "RequestModel" with
      | true, o -> task { return unbox<'a> o }
      | _ ->
        ctx.BindModelAsync<'a>()

    let getModelCustom<'a> (ctx: HttpContext) serializer culture =
      let clt = culture |> Option.map System.Globalization.CultureInfo.CreateSpecificCulture
      match serializer, clt with
      | Some s, Some c -> ctx.BindModelAsync<'a>(s,c)
      | None, Some c -> ctx.BindModelAsync<'a>(cultureInfo = c)
      | Some s, None -> ctx.BindModelAsync<'a>(settings = s)
      | None, None -> ctx.BindModelAsync<'a>()

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
