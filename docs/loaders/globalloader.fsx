#r "../_lib/Fornax.Core.dll"

type SiteInfo = {
    title: string
    description: string
    theme_variant: string option
    root_url: string
}

let config = {
    title = "Saturn"
    description = "Saturn - F# MVC Web Framework built on top of ASP .Net Core"
    theme_variant = Some "blue"
    root_url =
      #if WATCH
        "http://localhost:8080/"
      #else
        "https://saturnframework.org/"
      #endif
}

let loader (projectRoot: string) (siteContet: SiteContents) =
    siteContet.Add(config)

    siteContet
