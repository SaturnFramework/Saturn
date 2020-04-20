---
title: Saturn Overview
category: explanation
menu_order: 1
---

# Saturn Overview

Saturn is a web development framework written in F# which implements the server-side MVC pattern. Many of its components and concepts will seem familiar to those of us with experience in other web frameworks like Ruby on Rails or Python’s Django. It's heavily inspired by Elixir's [Phoenix](http://phoenixframework.org/).

Built on top of the battle-tested ASP.NET Core foundation and the highly flexible, extendable model of Giraffe, Saturn provides high level abstractions, helpers and tools to enable high developer productivity, at the same time keeping high application performance provided by Kestrel and Giraffe.

Saturn is made up of a number of distinct parts, each with its own purpose and role to play in building a web application.

 - Application
    - the start and end of the request life cycle
    - handles all aspects of requests up until the point where the router takes over
    - provides a core set of plugs to apply to all requests
    - dispatches requests into a router
    - enables application and hosting configuration
 - Router
    - parses incoming requests and dispatches them to the correct controller/action, passing parameters as needed
    - provides helpers to generate route paths or urls to resources
    - defines named pipelines through which we may pass our requests
    - allows easy application of groups of plugs to a set of routes
 - Controllers
    - provide functions, called *actions*, to handle requests
    - actions:
        - prepare data and pass it into views
        - invoke rendering via views
        - perform redirects
        - return data as JSON or XML
        - and much more
 - Views
    - render templates
    - act as a presentation layer
    - define helper functions, available in templates, to decorate data for presentation
 - Channels
    - manage sockets for easy realtime communication
    - are analogous to controllers except that they allow bi-directional communication with persistent connections
 - Scaffolding scripts
    - `dotnet new` template providing good starting point for new applications - [https://github.com/SaturnFramework/Saturn.Template](https://github.com/SaturnFramework/Saturn.Template)
    - `dotnet saturn` CLI tool that controls migrations and let you easily scaffold new parts of application - [https://github.com/SaturnFramework/Saturn.Dotnet](https://github.com/SaturnFramework/Saturn.Dotnet)


### Saturn rings

Saturn itself is the top layer of a multi-layer system designed to create a flexible, productive environment for creating web applications.

#### Kestrel and ASP.NET Core

ASP.NET Core is a cross-platform, high-performance, open-source framework for building modern, cloud-based, Internet-connected applications. Kestrel is a cross-platform web server for ASP.NET Core based on `libuv`, a cross-platform asynchronous I/O library

#### [Giraffe](https://github.com/giraffe-fsharp/Giraffe)

Giraffe is an F# micro web framework for building rich web applications. It has been heavily inspired and is similar to [Suave](https://suave.io), but has been specifically designed with ASP.NET Core in mind and can be plugged into the ASP.NET Core pipeline via middleware. Giraffe applications are composed of so-called HttpHandler functions which can be thought of as a mixture of Suave's WebParts and ASP.NET Core's middleware.

### Saturn moons

Saturn is not only a library building on top of Giraffe, but also a set of opinionated tooling for scaffolding a whole project and then generating some boilerplate code. At the moment our template is using (by default):

#### [Dapper](https://github.com/StackExchange/Dapper)

A simple, focused on performance object mapper for .Net that you can add in to your project and will extend your `IDbConnection` interface.

#### [Simple.Migrations](https://github.com/canton7/Simple.Migrations)

Simple.Migrations is a simple bare-bones migration framework for .NET Core (.NET Standard 1.2 and .NET 4.5). It doesn't provide SQL generation, nor an out-of-the-box command-line tool, nor any other fancy feature. It does however provide a set of simple, extendable, and composable tools for integrating migrations into your application.



