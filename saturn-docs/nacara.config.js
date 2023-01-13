// For more information about the config, please visit:
// https://mangelmaxime.github.io/Nacara/nacara/configuration.html
export default {
    "siteMetadata": {
        "url": "https://saturnframework.org",
        "baseUrl": "/",
        "editUrl" : "https://github.com/SaturnFramework/Saturn",
        "title": "Saturn Docs",
        "favIcon": "static/img/icon.png",
        "serverPort": 8080,
    },
    "navbar": {
        "start": [
            {
                "pinned": true,
                "label": "Home",
                "url": "/"
            },
            {
                "pinned": true,
                "label": "Guides",
                "items": [
                    {
                        "section": "getting-started",
                        "label": "Getting Started",
                        "url": "/contents/tutorials/how-to-start.html"
                    },
                    {
                        "section": "adding-saturn-to-giraffe",
                        "label": "Adding Saturn to Giraffe",
                        "url": "/contents/tutorials/adding-saturn-to-an-existing-giraffe-app.html"
                    },
                    {
                        "section": "adding-pages",
                        "label": "Adding Pages",
                        "url": "/contents/tutorials/adding-pages.html"
                    }
                ]
            },
            {
                "pinned": true,
                "label": "Documentation",
                "items": [
                    {
                        "section": "saturn-overview",
                        "label": "Saturn Overview",
                        "url": "/contents/explanations/overview.html",
                    },
                    "divider",
                    {
                        "section": "directory-structure",
                        "label": "Directory Structure",
                        "url": "/contents/explanations/directory-structure.html",
                    },
                    "divider",
                    {
                        "section": "scaffolding",
                        "label": "Scaffolding",
                        "url": "/contents/explanations/scaffolding.html",
                    },
                ]
            }
        ],
        "end": [
            {
                "url": "https://github.com/SaturnFramework/Saturn",
                "icon": "fab fa-github",
                "label": "Github"
            }
        ]
    },
    "remarkPlugins": [
        {
            "resolve": "gatsby-remark-vscode",
            "property": "remarkPlugin",
            "options": {
                "theme": "Atom One Light",
                "extensions": [
                    "vscode-theme-onelight"
                ]
            }
        }
    ],
    "layouts": [
        "nacara-layout-standard"
    ]
}