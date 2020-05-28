namespace Saturn

///Module containing helpers for CSRF Antiforgery protection
module CSRF =
  open FSharp.Control.Tasks.ContextInsensitive
  open Giraffe.Core
  open Giraffe.ResponseWriters
  open Microsoft.AspNetCore.Antiforgery
  open Microsoft.AspNetCore.Http
  open Microsoft.Extensions.Logging
  open System.Threading.Tasks

  let private shouldValidate  =
    let unprotectedMethods = Set.ofList ["GET"; "HEAD"; "TRACE"; "OPTIONS"]
    fun (ctx: HttpContext) -> not <| unprotectedMethods.Contains ctx.Request.Method

  type CSRFError =
  | NotConfigured
  | Invalid of error: AntiforgeryValidationException

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


  let inline private sendException (ex: CSRFError): HttpHandler = fun next ctx ->
    match ex with
    | NotConfigured ->
      logMissingAntiforgeryFeature ctx
      setStatusCode 500 next ctx
    | Invalid ex ->
      (setStatusCode 403
       >=> text ex.Message) next ctx

  let private validateCSRF (ctx: HttpContext): Task<Result<unit, CSRFError>> =
    task {
      try
        match ctx.GetService<IAntiforgery>() with
        | null ->
          return Error NotConfigured
        | antiforgery ->
          do! antiforgery.ValidateRequestAsync(ctx)
          return Ok ()
      with
      | :? AntiforgeryValidationException as ex ->
        return Error (Invalid ex)
    }

  /// Protect a resource by validating that requests that can change state come with a valid request antiforgery token, which is based off of a known session token.
  /// The particular configuration options can be set via the `application` builder's `use_antiforgery_with_config` method.
  /// If the request is not valid, a custom error handler will be invoked with the validation error
  let tryCsrf (errorHandler: (CSRFError -> HttpHandler)): HttpHandler = fun (next) (ctx) ->
    if shouldValidate ctx
    then
      task {
        match! validateCSRF ctx with
        | Ok () -> return! next ctx
        | Error reason -> return! errorHandler reason next ctx
      }
    else
      next ctx

  /// Protect a resource by validating that requests that can change state come with a valid request antiforgery token, which is based off of a known session token.
  /// The particular configuration options can be set via the `application` builder's `use_antiforgery_with_config` method.
  let csrf : HttpHandler = tryCsrf sendException

  type HttpContext with

    /// Protect a resource by validating that requests that can change state come with a valid request antiforgery token, which is based off of a known session token.
    /// The particular configuration options can be set via the `application` builder's `use_antiforgery_with_config` method.
    /// If the request is not valid, an exception will be thrown with details
    member x.ValidateCSRF() = task {
      match! validateCSRF x with
      | Ok () -> return ()
      | Error NotConfigured ->
        logMissingAntiforgeryFeature x
        return failwith "Not correctly configured for Antiforgery"
      | Error (Invalid reason) ->
        return raise reason
    }

    /// Protect a resource by validating that requests that can change state come with a valid request antiforgery token, which is based off of a known session token.
    /// The particular configuration options can be set via the `application` builder's `use_antiforgery_with_config` method.
    /// If the request is not valid, an Error result will be returned with details
    member x.TryValidateCSRF() = validateCSRF x


  let getRequestTokens (ctx: HttpContext) = ctx.GetService<IAntiforgery>().GetAndStoreTokens(ctx)

  //TODO
  // /// Contains view helpers for csrf tokens for various view engines.
  // module View =
  //   module Giraffe =
  //     open Giraffe.GiraffeViewEngine

  //     ///Creates a csrf token form input of the kind: <input type="hidden" name="TOKEN_NAME" value="TOKEN_VALUE" />
  //     let csrfTokenInput (ctx: HttpContext) =
  //       match ctx.GetService<IAntiforgery>() with
  //       | null ->
  //         logMissingAntiforgeryFeature ctx
  //         input [ _name "Missing Antiforgery Feature"
  //                 _value "No Antiforgery Token Available, check your application configuration"
  //                 _type "text" ]
  //       | antiforgery ->
  //         let tokens = antiforgery.GetAndStoreTokens(ctx)
  //         input [ _name tokens.FormFieldName
  //                 _value tokens.RequestToken
  //                 _type "hidden" ]

  //     ///View helper for creating a form that implicitly inserts a CSRF token hidden form input.
  //     let protectedForm ctx attrs children = form attrs (csrfTokenInput ctx :: children)
