#r "../_lib/Fornax.Core.dll"

type ApiReferences = {
    title: string
    link: string
}

type Shortcut = {
    title: string
    link: string
    icon: string
}

let loader (projectRoot: string) (siteContet: SiteContents) =
    siteContet.Add({title = "API reference"; link = "/Reference/ApiRef.html"})
    siteContet.Add({title = "Home"; link = "/"; icon = "fas fa-home"})
    siteContet.Add({title = "GitHub repo"; link = "TODO: ADD_LINK"; icon = "fab fa-github"})
    siteContet.Add({title = "License"; link = "/license.html"; icon = "far fa-file-alt"})
    siteContet