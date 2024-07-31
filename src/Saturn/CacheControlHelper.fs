namespace Saturn

[<AutoOpen>]
module CacheControls =

  /// https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Cache-Control
  /// response header values for cache-control
  type CacheControl =
    | NoCache
    | NoStore
    | NoTransform
    | StaleIfError
    | Immutable
    | MaxAge of int
    | MustRevalidate
    | MustUnderstand
    | Private
    | ProxyRevalidate
    | Public
    | SMaxage of int
    | StaleWhileRevalidate

  let generate (cacheControls: CacheControl list) = 

    let mapper = function
      | NoCache -> "no-cache"
      | NoStore -> "no-store"
      | NoTransform -> "no-transform"
      | StaleIfError -> "stale-if-error"
      | Immutable -> "immutable"
      | MaxAge age-> $"max-age={age}"
      | MustRevalidate -> "must-revalidate"
      | MustUnderstand -> "must-understand"
      | Private -> "private"
      | ProxyRevalidate -> "proxy-revalidate"
      | Public -> "public"
      | SMaxage age-> $"s-maxage={age}"
      | StaleWhileRevalidate -> "stale-while-revalidate"

    cacheControls 
    |> List.map mapper
    |> List.toArray 
    |> Microsoft.Extensions.Primitives.StringValues