/// <summary>Test response cache values sent to client</summary>
module CacheTests

open Giraffe
open Saturn.Endpoint
open Saturn.CacheControls
open System.Collections.Generic
open System


//---------------------------`Response.accepted` tests----------------------------------------
let testCiRouter = router {
  get "/static/index.html" (htmlFile"/static/index.html")
}

// //https://developer.mozilla.org/en-US/docs/Web/HTTP/Status
let nonErrorStatusCodes = seq { 100..299 }

[<Expecto.Tests>]
let tests =
  Expecto.Tests.testList "use_static Response.accepted cache tests" [
    Expecto.Tests.testCase "Correct status code not overloaded" <| fun _ ->
      let emptyContext = getEmptyContext "GET" "/static/index.html"
      let maxAge = 3600

      let cacheValues =  [ ]
      let context = testHostWithContext (hostFromControllerCached testCiRouter cacheValues) emptyContext

      let body = getBody' context
      Expecto.Expect.stringStarts body "<h1>Hello from static</h1>" "Should be a cached"

      let headers = 
        Dictionary(context.Response.Headers, StringComparer.OrdinalIgnoreCase)

      Expecto.Expect.isFalse (headers.ContainsKey "cache-control") "Cache header not as expected"
      Expecto.Expect.contains nonErrorStatusCodes context.Response.StatusCode "Should be a cachable code status"
    
    Expecto.Tests.testCase "Correct status code overloaded" <| fun _ ->
      let emptyContext = getEmptyContext "GET" "/static/index.html"
      let maxAge = 3600
      let cacheValues =  [ CacheControl.Public; CacheControl.MaxAge 3600 ]
      let context = testHostWithContext (hostFromControllerCached testCiRouter cacheValues) emptyContext

      let body = getBody' context
      Expecto.Expect.stringStarts body "<h1>Hello from static</h1>" "Should be a cached"

      let headers = 
        Dictionary(context.Response.Headers, StringComparer.OrdinalIgnoreCase)

      Expecto.Expect.isTrue (headers.ContainsKey "cache-control") "Cache header not as expected"
      Expecto.Expect.containsAll (headers["cache-control"]) [|"public"; $"max-age={maxAge}"|] "Cache header value not as expected"
      Expecto.Expect.contains nonErrorStatusCodes context.Response.StatusCode "Should be a cacheable code status"      
  ]

