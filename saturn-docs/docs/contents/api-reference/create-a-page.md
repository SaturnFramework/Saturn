---
title: Create a Page
layout: standard
---

A page in Nacara is a **Markdown** file composed of two things:

- **Front Matter**: configure how the page should rendered, for example it is here that you specify which layout to applied
- **Content**: The markdown content to include in the page

## Create your first page

Create a file `docs/documentation/guides/my-page.md`:

```
---
title: My page
layout: standard
---

This is a new page created for the tutorial.
```

The new page is available at [http://localhost:8080/documentation/guides/my-page.html](http://localhost:8080/documentation/guides/my-page.html)

## Add your page to the menu

If you look on the menu in the left, you will see that your page is missing from it.

This is because you need to provide some information to Nacara via the `menu.json` file.

Edit the file `docs/documentation/menu.json` to add `"documentation/guides/my-page"` to it.

The file should looks like:

```json
[
    {
        "type": "section",
        "label": "Overview",
        "items": [
            "documentation/introduction"
        ]
    },
    {
        "type": "section",
        "label": "Guides",
        "items": [
            "documentation/guides/create-a-page",
            "documentation/guides/customize-the-style",
            "documentation/guides/create-a-section",
            "documentation/guides/custom-layout",
            "documentation/guides/deploy-your-site"
        ]
    },
    "documentation/guides/my-page"
]

```

You should now see `My page` at the bottom of the menu.
