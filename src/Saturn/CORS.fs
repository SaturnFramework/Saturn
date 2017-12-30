namespace Saturn

module CORS =

  open Utils
  open Giraffe.HttpHandlers

  [<Literal>]
  let private Origin = "Origin"

  [<Literal>]
  let private AccessControlRequestMethod = "Access-Control-Request-Method"

  [<Literal>]
  let private AccessControlRequestHeaders = "Access-Control-Request-Headers"

  [<Literal>]
  let private AccessControlAllowOrigin = "Access-Control-Allow-Origin"

  [<Literal>]
  let private AccessControlAllowMethods = "Access-Control-Allow-Methods"

  [<Literal>]
  let private AccessControlAllowHeaders = "Access-Control-Allow-Headers"

  [<Literal>]
  let private AccessControlAllowCredentials = "Access-Control-Allow-Credentials"

  [<Literal>]
  let private AccessControlExposeHeaders = "Access-Control-Expose-Headers"

  [<Literal>]
  let private AccessControlMaxAge = "Access-Control-Max-Age"

  /// The configuration values for CORS
  type CORSConfig =
    { /// The list of allowed Uri(s) for requests.
      allowedUris             : InclusiveOption<string list>
      /// The list of allowed HttpMethods for the request.
      allowedMethods          : InclusiveOption<System.Net.Http.HttpMethod list>
      /// Allow cookies? This is sent in the AccessControlAllowCredentials header.
      allowCookies            : bool
      /// The list of response headers exposed to client. This is sent in AccessControlExposeHeaders header.
      exposeHeaders           : InclusiveOption<string list>
      /// Max age in seconds the user agent is allowed to cache the result of the request.
      maxAge                  : int option }

  let private isAllowedOrigin config (value : string) =
    match config.allowedUris with
    | InclusiveOption.All ->
      true

    | InclusiveOption.None ->
      false

    | InclusiveOption.Some uris ->
      uris
      |> List.exists (String.equalsCaseInsensitive value)

  let private setMaxAgeHeader config : HttpHandler =
    match config.maxAge with
    | None ->
      succeed

    | Some age ->
      setHttpHeader AccessControlMaxAge (age.ToString())

  let private setAllowCredentialsHeader config =
    if config.allowCookies then
        setHttpHeader AccessControlAllowCredentials "true"
    else
        succeed

  let private setAllowMethodsHeader config value =
    match config.allowedMethods with
    | InclusiveOption.None ->
      succeed

    | InclusiveOption.All ->
      setHttpHeader AccessControlAllowMethods "*"

    | InclusiveOption.Some (m :: ms) ->
      let exists = m.ToString() = value || List.exists (fun m -> m.ToString() = value) ms
      if exists then
        let header = sprintf "%s,%s" (m.ToString()) (ms |> Seq.map (fun i -> i.ToString()) |> String.concat( ", "))
        setHttpHeader AccessControlAllowMethods header
      else
        succeed

    | InclusiveOption.Some ([]) ->
      succeed

  let private setAllowOriginHeader value =
    setHttpHeader AccessControlAllowOrigin value

  let private setExposeHeadersHeader config =
    match config.exposeHeaders with
    | InclusiveOption.None
    | InclusiveOption.Some [] ->
      succeed
    | InclusiveOption.All ->
      setHttpHeader AccessControlExposeHeaders "*"
    | InclusiveOption.Some hs ->
      let header = hs |> String.concat(", ")
      setHttpHeader AccessControlExposeHeaders header

  let cors (config : CORSConfig) : HttpHandler =
    fun (nxt) (ctx) ->
      let req = ctx.Request
      match req.Headers.TryGetValue (Origin.ToLowerInvariant()) with
      | true, originValue -> // CORS request
        let allowedOrigin = isAllowedOrigin config (originValue.[0])
        match req.Method with
        | "OPTIONS" ->
          match req.Headers.TryGetValue  (AccessControlRequestMethod.ToLowerInvariant()) with
          | true, requestMethodHeaderValue -> // Preflight request
            // Does the request have an Access-Control-Request-Headers header? If so, validate. If not, proceed.
            let setAccessControlRequestHeaders =
              match req.Headers.TryGetValue (AccessControlRequestHeaders.ToLowerInvariant()) with
              | true, list ->
                setHttpHeader AccessControlAllowHeaders (list |> String.concat ", ")
              | _ ->
                succeed

            if allowedOrigin then
              let composed =
                setAllowMethodsHeader config requestMethodHeaderValue.[0]
                >=> setAccessControlRequestHeaders
                >=> setMaxAgeHeader config
                >=> setAllowCredentialsHeader config
                >=> setAllowOriginHeader originValue
                >=> setStatusCode 204
                >=> setBody [||]
              composed nxt ctx
            else
              succeed nxt ctx

          | _ ->
            succeed nxt ctx

        | _ ->
          if allowedOrigin then
            let composed =
              setExposeHeadersHeader config
              >=> setAllowCredentialsHeader config
              >=> setAllowOriginHeader originValue
              >=> setAllowMethodsHeader config "*"
            composed nxt ctx
          else
            succeed nxt ctx // No headers will be sent. Browser will deny.

      | _ ->
        nxt ctx // Not a CORS request


  let defaultCORSConfig =
    { allowedUris = InclusiveOption.All
      allowedMethods = InclusiveOption.All
      allowCookies = true
      exposeHeaders = InclusiveOption.None
      maxAge = None }
