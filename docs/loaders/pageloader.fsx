#r "../_lib/Fornax.Core.dll"

type Shortcut = {
    title: string
    link: string
    icon: string
}

let loader (projectRoot: string) (siteContet: SiteContents) =
    siteContet.Add({title = "Home"; link = "/"; icon = "fas fa-home"})
    siteContet.Add({title = "SAFE Stack"; link = "https://safe-stack.github.io/"; icon = "fas fa-cubes"})
    siteContet.Add({title = "GitHub repo"; link = "https://github.com/SaturnFramework/Saturn"; icon = "fab fa-github"})
    siteContet.Add({title = "License"; link = "/license.html"; icon = "far fa-file-alt"})
    siteContet

