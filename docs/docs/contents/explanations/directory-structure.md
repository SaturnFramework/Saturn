---
title: Directory Structure
layout: standard
---

# Directory Structure

After creating a new Saturn project, let's take a deeper look into what files are created.

```bash
├── .fake
├── .paket
├── .vs
├── .packages
├── paket-files
├── src
|   ├── Migrations
|   └── SaturnSample
├── .gitignore
├── build.fsx
├── global.json
├── paket.dependencies
├── paket.lock
└── SaturnSample.sln
```

At this level most of it is basic configuration files to help with running Saturn. From looking at the `.paket`, `paket-files`, `paket.dependencies`, `paket.lock` folders and files, you can see that by default Saturn uses [paket](https://fsprojects.github.io/Paket/) to handle package management. You won't be working with these files directly but rather through the command line tools instead.

Saturn also uses [FAKE](https://fake.build/) to build the project. You can see how it is set up by looking at the `.fake` folder and `build.fsx` file.

Other than this, Saturn provides a `.gitignore` file that prevents some folders from being tracked by git when they don't need to.

Lastly, there is the `SaturnSample.sln` solution file so you can open the project in an IDE like Visual Sudio and a `global.json` file to configure the solution file.

### Project structure

Most of the work you will do in this project however, will be in `src/SaturnSample`, which looks like the following when expanded:

```bash
  ├── bin
  ├── Books
  |   ├── BooksController.fs
  |   ├── BooksModel.fs
  |   ├── BooksRepository.fs
  |   └── BooksView.fs
  ├── obj
  ├── static
  |   ├── app.css
  |   └── app.js
  ├── Templates
  |   ├── App.fs
  |   ├── Index.fs
  |   ├── InternalError.fs
  |   └── NotFound.fs
  ├── Config.fs
  ├── Database.fs
  ├── database.sqlite
  ├── paket.references
  ├── Program.fs
  ├── Router.fs
  └── SaturnSample.fsproj
```

`bin` and `obj` folders store the compiled version of the program after you build the project.

The convention for Saturn is that the model and everything associated with it are inside one folder. Everything is also named with the plural form of the model so "Books" instead of "Book".

Your static files like css, js, and images should be inside the `static` folder.

`Config.fs` contains a `Config` record that stores settings that you can use inside your application. By default, the record only contains the `connectionString` field.

`Database.fs` contains functions to execute SQL queries within the program through [Dapper](https://stackexchange.github.io/Dapper/).

If you did not run `dotnet saturn migration` as in the [how to start guide](../tutorials/how-to-start.html), you might not see `database.sqlite`, but that is the database file that your Saturn project is using to store data.

`paket.references` shows the packages that your project is using. You can find more information about it in the [official documentation](https://fsprojects.github.io/Paket/references-files.html).

`Program.fs` handles intializing the program and loading up various settings.

`Router.fs` is where you will set the routes of the application, specifying what page to load for example.

Lastly, `SaturnSample.fsproj` is the project file itself. This file extension is related to the project file of MSBuild ([Microsoft documentation](https://docs.microsoft.com/en-us/aspnet/web-forms/overview/deployment/web-deployment-in-the-enterprise/understanding-the-project-file)).
