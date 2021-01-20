# Saturn

![Build](https://github.com/SaturnFramework/Saturn/workflows/Build/badge.svg) ![GitHub last commit](https://img.shields.io/github/last-commit/SaturnFramework/Saturn?style=flat-square) ![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Saturn?style=flat-square)

[![Open in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/SaturnFramework/Saturn)

Saturn is a web development framework written in F# which implements the server-side MVC pattern. Many of its components and concepts will seem familiar to anyone with experience in other web frameworks like Ruby on Rails or Python’s Django.

It's heavily inspired by Elixir's [Phoenix](http://phoenixframework.org/).

Read more about why I've decided to create Saturn, and some of its design choices on my blog - http://kcieslak.io/Reinventing-MVC-for-web-programming-with-F

## Documentation

Saturn has nice [documentation](https://saturnframework.org/explanations/overview.html) and I appreciate any help to improve it further by sending pull requests or just adding an issue with what you think is missing.

## How to build

1. Install the .NET 5.0 SDK from https://dotnet.microsoft.com/download/dotnet-core/5.0
2. Restore dotnet SDK tools: `dotnet tool restore`
3. Inside the repo directory, run `dotnet fake build`

## How to contribute

*Imposter syndrome disclaimer*: I want your help. No really, I do.

There might be a little voice inside that tells you you're not ready; that you need to do one more tutorial, or learn another framework, or write a few more blog posts before you can help me with this project.

I assure you, that's not the case.

This project has some clear Contribution Guidelines and expectations that you can [read here](https://github.com/Krzysztof-Cieslak/Saturn/blob/master/CONTRIBUTING.md).

The contribution guidelines outline the process that you'll need to follow to get a patch merged. By making expectations and process explicit, I hope it will make it easier for you to contribute.

And you don't just have to write code. You can help out by writing documentation, tests, or even by giving feedback about this work. (And yes, that includes giving feedback about the contribution guidelines.)

Thank you for contributing!


## Contributing and copyright

The project is hosted on [GitHub](https://github.com/Krzysztof-Cieslak/Saturn) where you can [report issues](https://github.com/Krzysztof-Cieslak/Saturn/issues), fork
the project and submit pull requests.

The library is available under [MIT license](https://github.com/Krzysztof-Cieslak/Saturn/blob/master/LICENSE.md), which allows modification and redistribution for both commercial and non-commercial purposes.

Please note that this project is released with a [Contributor Code of Conduct](CODE_OF_CONDUCT.md). By participating in this project you agree to abide by its terms.
