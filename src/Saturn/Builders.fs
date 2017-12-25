module Saturn.Router

open Giraffe.HttpHandlers
open Giraffe.TokenRouter
open System.Collections.Generic
open Microsoft.AspNetCore.Http

type PipelineBuilder internal () =
  member __.Yield(_) : HttpHandler =
    fun nxt cntx -> nxt cntx

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

[<RequireQualifiedAccess>]
type RouteType =
  | Get
  | Post
  | Put
  | Delete
  | Patch
  | Forward

type ScopeState =
  { Routes: Dictionary<string * RouteType, HttpHandler list>
    RoutesF: Dictionary<string * RouteType, (obj -> HttpHandler) list>

    NotFoundHandler: HttpHandler
    Pipelines: HttpHandler list
  }
  with
    member internal state.GetRoutes(typ: RouteType) =
      let rts =
        state.Routes
        |> Seq.map(|KeyValue|)
        |> Seq.filter(fun ((_, t), _) -> t = typ )
        |> Seq.map (fun ((p, _), acts) -> (p, acts |> List.rev))
      let rtsf =
        state.RoutesF
        |> Seq.map(|KeyValue|)
        |> Seq.filter(fun ((_, t), _) -> t = typ )
        |> Seq.map (fun ((p, _), (acts)) -> (p, acts |> List.rev))
      rts,rtsf

type ScopeBuilder internal () =

  let addRoute typ state path action : ScopeState =
    let action =  action |> List.foldBack (>=>) state.Pipelines
    let lst =
      match state.Routes.TryGetValue((path, typ)) with
      | false, _ -> []
      | true, lst -> lst
    state.Routes.[(path, typ)] <-  action::lst
    state

  let addRouteF typ state (path: PrintfFormat<_,_,_,_,'f>) action : ScopeState =
    let action = fun o -> (action o) |> List.foldBack (>=>) state.Pipelines
    let r = fun (o : obj) -> o |> unbox<'f> |> action
    let lst =
      match state.RoutesF.TryGetValue((path.Value, typ)) with
      | false, _ -> []
      | true, lst -> lst
    state.RoutesF.[(path.Value, typ)] <- r::lst
    state

  member __.Yield(_) : ScopeState =
    { Routes = Dictionary()
      RoutesF = Dictionary()
      Pipelines = []
      NotFoundHandler = setStatusCode 404 >=> text "Not found" }

  member __.Run(state : ScopeState) : HttpHandler =
    let generateRoutes typ =
      let routes, routesf = state.GetRoutes typ
      let routes = routes |> Seq.map (fun (p, lst) -> route p (choose lst))
      let routesf = routesf |> Seq.map (fun (p, lst) ->
        let pf = PrintfFormat<_,_,_,_,_> p
        let chooseF = fun o ->
          lst
          |> List.map (fun f -> f o)
          |> choose
        routefUnsafe pf chooseF
      )
      routes, routesf

    let gets, getsf = generateRoutes RouteType.Get
    let posts, postsf = generateRoutes RouteType.Post
    let puts, putsf = generateRoutes RouteType.Put
    let deletes, deletesf = generateRoutes RouteType.Put

    let forwards, _ = state.GetRoutes RouteType.Forward
    let forwards =
      forwards
      |> Seq.map (fun (p, lst) ->
        let act = ((fun nxt ctx ->
          let path = ctx.Request.Path.Value
          if path.StartsWith p then
            let newPath = path.Substring p.Length
            ctx.Request.Path <- PathString(newPath)
          nxt ctx)
          >=> choose lst )

        routef (PrintfFormat<_,_,_,_,string>(p + "%s")) (fun _ -> act ))

    let lst = [
      yield GET [
        yield! gets
        yield! getsf
        yield! forwards
      ]
      yield POST [
        yield! posts
        yield! postsf
        yield! forwards
      ]
      yield PUT [
        yield! puts
        yield! putsf
        yield! forwards
      ]
      yield DELETE [
        yield! deletes
        yield! deletesf
        yield! forwards
      ]
    ]
    router state.NotFoundHandler lst

  [<CustomOperation("get")>]
  member __.Get(state, path : string, action: HttpHandler) : ScopeState =
    addRoute RouteType.Get state path action

  [<CustomOperation("getf")>]
  member __.GetF(state, path : PrintfFormat<_,_,_,_,'f>, action) : ScopeState =
    addRouteF RouteType.Get state path action

  [<CustomOperation("post")>]
  member __.Post(state, path : string, action: HttpHandler) : ScopeState =
    addRoute RouteType.Post state path action

  [<CustomOperation("postf")>]
  member __.PostF(state, path, action) : ScopeState =
    addRouteF RouteType.Post state path action

  [<CustomOperation("put")>]
  member __.Put(state, path : string, action: HttpHandler) : ScopeState =
    addRoute RouteType.Put state path action

  [<CustomOperation("putf")>]
  member __.PutF(state, path, action) : ScopeState =
    addRouteF RouteType.Put state path action

  [<CustomOperation("delete")>]
  member __.Delete(state, path : string, action: HttpHandler) : ScopeState =
    addRoute RouteType.Delete state path action

  [<CustomOperation("deletef")>]
  member __.DeleteF(state, path, action) : ScopeState =
    addRouteF RouteType.Delete state path action

  [<CustomOperation("patch")>]
  member __.Patch(state, path : string, action: HttpHandler) : ScopeState =
    addRoute RouteType.Patch state path action

  [<CustomOperation("patchf")>]
  member __.PatchF(state, path, action) : ScopeState =
    addRouteF RouteType.Patch state path action

  [<CustomOperation("forward")>]
  member __.Forward(state, path : string, action : HttpHandler) : ScopeState =
    addRoute RouteType.Forward state path action

  [<CustomOperation("pipe_through")>]
  member __.PipeThrough(state, pipe) : ScopeState =
    {state with Pipelines = pipe::state.Pipelines}

  [<CustomOperation("error_handler")>]
  member __.ErrprHandler(state, handler) : ScopeState =
    {state with NotFoundHandler = handler}

let scope = ScopeBuilder()
