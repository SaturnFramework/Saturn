namespace Saturn

open FSharp.Control.Tasks.ContextInsensitive
open Giraffe.Core
open Giraffe.ResponseWriters
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http

module CSRF =
  let private shouldValidate  =
    let unprotectedMethods = Set.ofList ["GET"; "HEAD"; "TRACE"; "OPTIONS"]
    fun (ctx: HttpContext) -> not <| unprotectedMethods.Contains ctx.Request.Method

  let inline private sendException (ex: AntiforgeryValidationException) =
    setStatusCode 403
    >=> text ex.Message

  /// Protect a resource by validating that requests that can change state come with a valid request antiforgery token, which is based off of a known session token.
  /// The particular configuration options can be set via the `application` builder's `use_antiforgery_with_config` method.
  let csrf : HttpHandler =
    fun (next) (ctx) -> task {
     if shouldValidate ctx
     then
      try
        do! ctx.GetService<IAntiforgery>().ValidateRequestAsync(ctx)
        return! next ctx
      with
      | :? AntiforgeryValidationException as ex ->
        return! sendException ex next ctx
     else
      return! next ctx
    }

  let getRequestTokens (ctx: HttpContext) = ctx.GetService<IAntiforgery>().GetAndStoreTokens(ctx)


