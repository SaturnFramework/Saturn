namespace Saturn

module Static =

  open System
  open System.IO
  open System.Text.RegularExpressions
  open Giraffe.HttpHandlers
  open Microsoft.AspNetCore.Http


  type StaticConfig = {
    // UseGZip: bool
    // UseBrotli: bool
    Match : string
    CacheControlForVSN: string
    CacheControlForEtag: string
    EtagGeneration: string -> string
    Headers: (string * string) list
    ContentTyes: (string * string) list
  }

  type private FileStatus =
    | Stale
    | Fresh

  // If we serve gzip or brotli at any moment, we need to set the proper vary header regardless of whether we are serving gzip content right now.
  // See: http://www.fastly.com/blog/best-practices-for-using-the-vary-header/
  let private maybeAddVary (config : StaticConfig) =
  // if config.UseGZip || config.UseBrotli then
  //   setHttpHeader "vary" "Accept-Encoding"
  // else
        succeed

  let private putCacheHeader (path: string) (config : StaticConfig) (ctx : HttpContext) : (FileStatus * HttpHandler) =
    if ctx.Request.QueryString.HasValue && ctx.Request.QueryString.Value.StartsWith "vsn=" then
      Stale, (setHttpHeader "cache-control" config.CacheControlForVSN)
    else
      let etag = config.EtagGeneration path
      let conn =
        setHttpHeader "cache-control" config.CacheControlForEtag
        >=> setHttpHeader "etag" etag
      let ifNonMatchLst =
        match ctx.Request.Headers.TryGetValue "if-none-match" with
        | true, vals -> vals.ToArray()
        | _ -> [||]

      if ifNonMatchLst |> Array.contains etag || ifNonMatchLst |> Array.contains "*" then
        Fresh, conn
      else
        Stale, conn

  let sendFile path : HttpHandler =
    let cnt = File.ReadAllBytes path
    setBody cnt

  let private serveStatic path config (ctx : HttpContext) : HttpHandler =
    match putCacheHeader path config ctx with
    | Fresh, conn ->
      conn
      >=> setStatusCode 304
    | Stale, conn ->
      let ext = Path.GetExtension path
      let contentType =
        match config.ContentTyes |> List.tryFind (fun (k,_) -> k = ext) |> Option.map snd with
        | Some s -> s
        | None ->
          let mimes = get<(string * string) list> "MimeTypes" ctx
          match mimes |> Option.bind (List.tryFind (fun (k,_) -> k = ext)) |> Option.map snd with
          | Some s -> s
          | None -> ""
      conn
      >=> setHttpHeader "content-type" contentType
      >=> setHttpHeaders config.Headers
      >=> maybeAddVary config
      >=> setStatusCode 200
      >=> sendFile path

  let private defaultEtag path =
    let info = FileInfo path
    (info.LastWriteTimeUtc, info.Length).GetHashCode().ToString()

  let defaultConfig = {
    Match = "*"
    CacheControlForVSN = "public, max-age=31536000"
    CacheControlForEtag = "public"
    EtagGeneration = defaultEtag
    Headers = []
    ContentTyes =
      [
        ".html", "text/html"
      ]
  }
  let call (from : string) config (nxt : HttpFunc) (ctx : HttpContext) : HttpFuncResult=
    let path = ctx.Request.Path.Value.TrimStart '/'
    if ctx.Request.Method = "GET" || ctx.Request.Method = "HEAD" then
        let m = Regex.Escape(config.Match).Replace( @"\*", ".*" ).Replace( @"\?", "." )
        if Regex.IsMatch(path, m) then
          let p = Path.Combine(Path.GetDirectoryName(Diagnostics.Process.GetCurrentProcess().MainModule.FileName), from, path)
          if File.Exists p then
            (serveStatic p config ctx) nxt ctx
          else
            nxt ctx
        else
          nxt ctx
    else
      nxt ctx