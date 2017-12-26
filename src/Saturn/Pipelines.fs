namespace Saturn

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Extensions
open Giraffe.Tasks
open Giraffe.HttpContextExtensions
open System

module Pipeline =

  open Giraffe.HttpHandlers

  type PipelineBuilder internal () =
    member __.Yield(_) : HttpHandler = succeed

    ///`plug` enables adding any additional `HttpHandler` to the pipeline
    [<CustomOperation("plug")>]
    member __.Plug(state, plug) : HttpHandler  = state >=> plug

    ///`must_accept` filters a request by the `Accept` HTTP header. You can use it to check if a client accepts a certain mime type before returning a response.
    [<CustomOperation("must_accept")>]
    member __.MustAccept(state, accepts) : HttpHandler  = state >=> (mustAccept accepts)

    ///`challenge` challenges an authentication with a specified authentication scheme
    [<CustomOperation("challenge")>]
    member __.Challenge(state, scheme) : HttpHandler  = state >=> (challenge scheme)

    ///`sign_off` signs off the currently logged in user.
    [<CustomOperation("sign_off")>]
    member __.SignOff(state, scheme) : HttpHandler  = state >=> (signOff scheme)

    ///`requires_auth_policy` validates if a user satisfies policy requirement, if not then the handler will execute the `authFailedHandler` function.
    [<CustomOperation("requires_auth_policy")>]
    member __.RequiresAuthPolicy(state, check, authFailedHandler) : HttpHandler  = state >=> (requiresAuthPolicy check authFailedHandler)

    ///`requires_authentication` validates if a user is authenticated/logged in. If the user is not authenticated then the handler will execute the `authFailedHandler` function.
    [<CustomOperation("requires_authentication")>]
    member __.RequiresAuthentication (state, authFailedHandler) : HttpHandler  = state >=> (requiresAuthentication authFailedHandler)

    ///`requires_role` validates if an authenticated user is in a specified role. If the user fails to be in the required role then the handler will execute the `authFailedHandler` function.
    [<CustomOperation("requires_role")>]
    member __.RequiresRole (state, role, authFailedHandler) : HttpHandler  = state >=> (requiresRole role authFailedHandler)

    ///`requires_role_of` validates if an authenticated user is in one of the supplied roles. If the user fails to be in one of the required roles then the handler will execute the `authFailedHandler` function.
    [<CustomOperation("requires_role_of")>]
    member __.RequiresRoleOf (state, roles, authFailedHandler) : HttpHandler  = state >=> (requiresRoleOf roles authFailedHandler)

    ///`clear_response` tries to clear the current response. This can be useful inside an error handler to reset the response before writing an error message to the body of the HTTP response object.
    [<CustomOperation("clear_response")>]
    member __.ClearResponse (state) : HttpHandler  = state >=> clearResponse

    ///`set_status_code` changes the status code of the `HttpResponse`.
    [<CustomOperation("set_status_code")>]
    member __.SetStatusCode (state, code) : HttpHandler  = state >=> (setStatusCode code)

    ///`set_header` sets or modifies a HTTP header of the `HttpResponse`.
    [<CustomOperation("set_header")>]
    member __.SetHeader(state, key, value) : HttpHandler  = state >=> (setHttpHeader key value)

    ///`set_body` sets or modifies the body of the `HttpResponse`. This http handler triggers a response to the client and other http handlers will not be able to modify the HTTP headers afterwards any more.
    [<CustomOperation("set_body")>]
    member __.SetBody(state, value) : HttpHandler  = state >=> (setBody value)

    ///`set_body` sets or modifies the body of the `HttpResponse`. This http handler triggers a response to the client and other http handlers will not be able to modify the HTTP headers afterwards any more.
    [<CustomOperation("set_body")>]
    member __.SetBody(state, value) : HttpHandler  = state >=> (setBodyAsString value)

    ///`text` sets or modifies the body of the `HttpResponse` by sending a plain text value to the client. This http handler triggers a response to the client and other http handlers will not be able to modify the HTTP headers afterwards any more. It also sets the `Content-Type` HTTP header to `text/plain`.
    [<CustomOperation("text")>]
    member __.Text(state, cnt) : HttpHandler  = state >=> (text cnt)

    ///`json` sets or modifies the body of the `HttpResponse` by sending a JSON serialized object to the client. This http handler triggers a response to the client and other http handlers will not be able to modify the HTTP headers afterwards any more. It also sets the `Content-Type` HTTP header to `application/json`.
    [<CustomOperation("json")>]
    member __.Json(state, serializer, cnt) : HttpHandler  = state >=> (customJson serializer cnt)

    ///`json` sets or modifies the body of the `HttpResponse` by sending a JSON serialized object to the client. This http handler triggers a response to the client and other http handlers will not be able to modify the HTTP headers afterwards any more. It also sets the `Content-Type` HTTP header to `application/json`.
    [<CustomOperation("json")>]
    member __.Json(state, cnt) : HttpHandler  = state >=> (json cnt)

    ///`xml` sets or modifies the body of the `HttpResponse` by sending an XML serialized object to the client. This http handler triggers a response to the client and other http handlers will not be able to modify the HTTP headers afterwards any more. It also sets the `Content-Type` HTTP header to `application/xml`.
    [<CustomOperation("xml")>]
    member __.Xml(state, cnt) : HttpHandler  = state >=> (xml cnt)

    ///`negotiate` sets or modifies the body of the `HttpResponse` by inspecting the `Accept` header of the HTTP request and deciding if the response should be sent in JSON or XML or plain text. If the client is indifferent then the default response will be sent in JSON. This http handler triggers a response to the client and other http handlers will not be able to modify the HTTP headers afterwards any more.
    [<CustomOperation("negotiate")>]
    member __.Negotiate(state, cnt) : HttpHandler  = state >=> (negotiate cnt)

    ///`negotiateWith` sets or modifies the body of the `HttpResponse` by inspecting the `Accept` header of the HTTP request and deciding in what mimeType the response should be sent. A dictionary of type `IDictionary<string, obj -> HttpHandler>` is used to determine which `obj -> HttpHandler` function should be used to convert an object into a `HttpHandler` for a given mime type. This http handler triggers a response to the client and other http handlers will not be able to modify the HTTP headers afterwards any more.
    [<CustomOperation("negotiate_with")>]
    member __.NegotiateWith(state, rules, unaccepted, cnt) : HttpHandler  = state >=> (negotiateWith rules unaccepted cnt)

    ///`html` sets or modifies the body of the `HttpResponse` with the contents of a single string variable. This http handler triggers a response to the client and other http handlers will not be able to modify the HTTP headers afterwards any more.
    [<CustomOperation("html")>]
    member __.Html(state, cnt) : HttpHandler  = state >=> (html cnt)

    ///`html_file` sets or modifies the body of the `HttpResponse` with the contents of a physical html file. This http handler triggers a response to the client and other http handlers will not be able to modify the HTTP headers afterwards any more. This http handler takes a rooted path of a html file or a path which is relative to the ContentRootPath as the input parameter and sets the HTTP header `Content-Type` to `text/html`.
    [<CustomOperation("html_file")>]
    member __.HtmlFile(state, fileName) : HttpHandler  = state >=> (htmlFile fileName)

    ///`render_html` is a more functional way of generating HTML by composing HTML elements in F# to generate a rich Model-View output.
    [<CustomOperation("render_html")>]
    member __.RenderHtml(state, cnt) : HttpHandler  = state >=> (renderHtml cnt)

    ///`redirect_to` uses a 302 or 301 (when permanent) HTTP response code to redirect the client to the specified location. It takes in two parameters, a boolean flag denoting whether the redirect should be permanent or not and the location to redirect to.
    [<CustomOperation("redirect_to")>]
    member __.RedirectTo(state, pernament, location) : HttpHandler  = state >=> (redirectTo pernament location)

    ///If your web server is listening to multiple ports then you can use the `routePorts` HttpHandler to easily filter incoming requests based on their port by providing a list of port number and HttpHandler (`(int * HttpHandler) list`).
    [<CustomOperation("route_ports")>]
    member __.RoutePorts(state, handlersByPorts) : HttpHandler = state >=> (routePorts handlersByPorts)

    ///If your route is not returning a static response, then you should wrap your function with a warbler. Functions in F# are eagerly evaluated and the warbler will help to evaluate the function every time the route is hit.
    [<CustomOperation("use_warbler")>]
    member __.Warbler(state : HttpHandler) : HttpHandler = warbler(fun _ -> state)

  ///`pipeline` computation expression is a way to create `HttpHandler` using composition of low-level helper functions.
  let pipeline = PipelineBuilder()

  let acceptJson : HttpHandler = mustAccept ["application/json"]

  let acceptXml : HttpHandler = mustAccept ["application/xml"]

  let acceptHtml : HttpHandler = mustAccept ["text/html"]

  let acceptMultipart : HttpHandler = mustAccept ["multipart/form-data"]

  /// Put headers that improve browser security.
  /// It sets the following headers:
  ///  * x-frame-options - set to SAMEORIGIN to avoid clickjacking through iframes unless in the same origin
  ///  * x-content-type-options - set to nosniff. This requires script and style tags to be sent with proper content type
  ///  * x-xss-protection - set to "1; mode=block" to improve XSS protection on both Chrome and IE
  ///  * x-download-options - set to noopen to instruct the browser not to open a download directly in the browser, to avoid HTML files rendering inline and accessing the security context of the application (like critical domain cookies)
  ///  * x-permitted-cross-domain-policies - set to none to restrict Adobe Flash Player’s access to data
  let putSecureBrowserHeaders : HttpHandler = pipeline {
      set_header "x-frame-option" "SAMEORIGIN"
      set_header "x-xss-protection" "1; mode=block"
      set_header "x-content-type-options" "nosniff"
      set_header "x-download-options" "noopen"
      set_header "x-permitted-cross-domain-policies" "none"
  }

  ///TODO: csrf protection - https://github.com/elixir-plug/plug/blob/v1.4.3/lib/plug/csrf_protection.ex
  let protectFromForgery : HttpHandler = failwith "Not implemented"

  ///Enables CORS pretection using provided config. Use `CORS.defaultCORSConfig` for default configuration.
  let enableCors config : HttpHandler = CORS.cors config

  ///Fetches session from session provider. If it won't be called session will be synchronusly fetched on first usage.
  let fetchSession (nxt : HttpFunc) (ctx : HttpContext) : HttpFuncResult =
    task {
      do! ctx.Session.LoadAsync()
      return! nxt ctx
    }

  ///Tries to model from request and puts model into `Items.RequestModel`. If it won't be called content can be fetched using `Context.Controller` helpers.
  ///It won't crash the pipelines if fetching failed.
  ///It optionally takes custom JSON deserializer settings, and/or culture name as arguments.
  let fetchModel<'a> serializer culture (nxt : HttpFunc) (ctx : HttpContext) : HttpFuncResult =
    let clt = culture |> Option.map System.Globalization.CultureInfo.CreateSpecificCulture
    try
      let mdl =
        match serializer, clt with
        | Some s, Some c -> ctx.BindModelAsync<'a>(s,c)
        | None, Some c -> ctx.BindModelAsync<'a>(cultureInfo = c)
        | Some s, None -> ctx.BindModelAsync<'a>(settings = s)
        | None, None -> ctx.BindModelAsync<'a>()
      ctx.Items.["RequestModel"] <- mdl
    with
    | _ -> ()
    nxt ctx

  let internal fetchUrl (nxt : HttpFunc) (ctx : HttpContext) : HttpFuncResult =
    if not (ctx.Items.ContainsKey "RequestUrl") then
      ctx.Items.["RequestUrl"] <- ctx.Request.GetDisplayUrl()

    nxt ctx

  ///Convert `HEAD` requests to `GET` requests.
  let head (nxt : HttpFunc) (ctx : HttpContext) : HttpFuncResult =
    if ctx.Request.Method = "HEAD" then ctx.Request.Method <- "GET"
    nxt ctx

  ///Pipeline for generating a unique request id for each request. A generated request id will in the format `uq8hs30oafhj5vve8ji5pmp7mtopc08f`.
  ///If a request id already exists as the `x-request-id` HTTP request header, then that value will be used assuming it is between 20 and 200 characters. If it is not, a new request id will be generated.
  ///Request id is put into `x-request-id` HTTP header and into `Items` directory of HttpContext with `RequestId` key.
  let requestId  (nxt : HttpFunc) (ctx : HttpContext) : HttpFuncResult =
    let reqId =
      match ctx.Request.Headers.TryGetValue "x-request-id" with
      | true, v when v.[0].Length >= 20 && v.[0].Length <= 200 -> v.[0]
      | _ -> Guid.NewGuid().ToString("N")
    ctx.Items.["RequestId"] <- reqId
    setHttpHeader "x-request-id" reqId nxt ctx

  ///TODO: force SSL connections - https://github.com/elixir-plug/plug/blob/v1.4.3/lib/plug/ssl.ex#L1
  let ssl : HttpHandler = failwith "Not implemented"
