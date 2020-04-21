---
title: Scaffolding
category: explanation
menu_order: 3
---

# Scaffolding

Saturn provides a command line scaffolding tool to generate a model, database migration, controller, and associated views.

To use the generator, run the `dotnet saturn` command from the root of your application application.  If you have used the template to generate your project, this is the directory with `build.fsx` or `paket.dependencies` file.

There are three flavors of generators that provide flexibility about what types of code is generated to support your model.

| Command      |  Generates                                                                     |
|--------------|--------------------------------------------------------------------------------|
| gen          | Creates a model, database layer, HTML views, and a controller.                 |
| gen.json     | Creates a model, database layer, and an API controller to access the model.    |
| gen.model    | Creates only the model and database layer (no controller or views)             |

Each of these commands will generate a migration for your model as well as a folder containing generated files.

For example:

`dotnet saturn gen Book Books id:string title:string`

Generates the following structure:

```
src
├── Migrations
│   └── 201903192143.Book.fs
│
└── SaturnSample
    └── Books
        ├── BooksController.fs
        ├── BooksModel.fs
        ├── BooksRepository.fs
        └── BooksViews.fs
```

Each of the generators takes arguments in the same format:

`dotnet saturn gen <SingularModelName> <PluralModelName> <List of model fields with types>`

The list of model fields are names and types separated by a colon.

`<fieldname>:<type>`

Currently supported types are:

* string
* int
* float
* double
* decimal
* guid
* datetime
* bool

## Migrations

Using the generator to create a model will also create a migration file that will create a supporting table in the database. Execute the migration script using:

`dotnet saturn migration`