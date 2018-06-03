namespace Saturn

module CSRF =
  open FSharp.Control.Tasks.ContextInsensitive
  open Giraffe.Core
  open Giraffe.ResponseWriters
  open Microsoft.AspNetCore.Antiforgery
  open Microsoft.AspNetCore.Http
  open Microsoft.Extensions.Logging

  let private shouldValidate  =
    let unprotectedMethods = Set.ofList ["GET"; "HEAD"; "TRACE"; "OPTIONS"]
    fun (ctx: HttpContext) -> not <| unprotectedMethods.Contains ctx.Request.Method

  let inline private sendException (ex: AntiforgeryValidationException) =
    setStatusCode 403
    >=> text ex.Message

  let inline private logMissingAntiforgeryFeature (ctx: HttpContext) =
    let logger = ctx.GetService<ILoggerFactory>().CreateLogger("Saturn.CSRF")
    logger.LogWarning(
      """The `IAntiforgery` feature could not be resolved. Are you sure you have added it to the current application? For example:
  application {
    use_antiforgery
  },
or
  application {
    use_antiforgery_with_config (fun options -> options.HeaderName <- "X-XSRF-TOKEN")
  }
      """)
  /// Protect a resource by validating that requests that can change state come with a valid request antiforgery token, which is based off of a known session token.
  /// The particular configuration options can be set via the `application` builder's `use_antiforgery_with_config` method.
  let csrf : HttpHandler =
    fun (next) (ctx) -> task {
     if shouldValidate ctx
     then
      try
        match ctx.GetService<IAntiforgery>() with
        | null ->
          logMissingAntiforgeryFeature ctx
          return! setStatusCode 500 next ctx
        | antiforgery ->
          do! antiforgery.ValidateRequestAsync(ctx)
          return! next ctx
      with
      | :? AntiforgeryValidationException as ex ->
        return! sendException ex next ctx
     else
      return! next ctx
    }

  let getRequestTokens (ctx: HttpContext) = ctx.GetService<IAntiforgery>().GetAndStoreTokens(ctx)

  /// Contains view helpers for csrf tokens for various view engines.
  module View =
    module Giraffe =
      open Giraffe.GiraffeViewEngine

      ///Creates a csrf token form input of the kind: <input type="hidden" name="TOKEN_NAME" value="TOKEN_VALUE" />
      let csrfTokenInput (ctx: HttpContext) =
        match ctx.GetService<IAntiforgery>() with
        | null ->
          logMissingAntiforgeryFeature ctx
          input [ _name "Missing Antiforgery Feature"
                  _value "No Antiforgery Token Available, check your application configuration"
                  _type "text" ]
        | antiforgery ->
          let tokens = antiforgery.GetAndStoreTokens(ctx)
          input [ _name tokens.FormFieldName
                  _value tokens.RequestToken
                  _type "hidden" ]

      ///View helper for creating a form that implicitly inserts a CSRF token hidden form input.
      let protectedForm ctx attrs children = form attrs (csrfTokenInput ctx :: children)
