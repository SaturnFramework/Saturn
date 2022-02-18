# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.16.0-preview02] - 18.02.2022

### Changed

* Fix build file

## [0.16.0-preview01] - 17.02.2022

### Changed

* Updated to Giraffe 6.0 prerelease
* Updated to net6.0 and ASP.NET Core 6
* Removed Ply and used new FSharp task{} CE

## [0.15.0] - 09.06.2021

### Added

* `add use_response_caching` to application CE (by [@groma84](https://github.com/groma84))
* Log prematurely closed connections as info, not as error (by [@retendo](https://github.com/retendo))
* Added DI support for all CEs we provide - the `_di` versions of custom operations are avaliable in following modules: `ApplicationDI`, `ChannelsDI`, `ControllerDI` (both standard, and endpoint routing), `PipelinesDI`, and `RouterDI` (both standard, and endpoint routing) (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak) and [@Arshia001](https://github.com/Arshia001))
* Added controller versioning for endpoint routing (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Added `RouterEndpoint`, `ControllerEndpoint` modules allowing to create routing using ASP.NET Endpoint Routing
* Added `use_endpoint_router` to `application` computation expression allowing to use Endpoint Routing in the application
* [Infrastructure] Add performance benchmark for Saturn using Endpoint Routing


### Fixed

* Preserve stack trace by default in controller (by [@retendo](https://github.com/retendo))
* Fixes exception propagation when using channels (by [@retendo](https://github.com/retendo))
* Fix typo getConfiguration (by [@kaashyapan](https://github.com/kaashyapan))
* Fix putSecureBrowserHeaders header typo (by [@Shmew](https://github.com/Shmew))
* Fix application/json not being compressed in response (by [@may-day](https://github.com/may-day))
* Include querystring in Turbolinks-location (by [@viktorvan](https://github.com/viktorvan))


### Changed

* Updated to Giraffe 5.0
* Updated to net5.0 and ASP.NET Core 5
* Moved to `Giraffe.ViewEngine` package for view rendering support

## [0.14.1] - 18.06.2020

### Added

* Helpers for getting `IWebHostEnvironment` and `IConfiguration` in context of `application` CE (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* [Infrastructure] Add performance benchmark using wrk and GitHub Action to run it

### Changed

* Use `IWebHostEnvironment` internally (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Move application CE to `IHostBuilder` (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* [Infrastructure] Moved to CHANGELOG.md from RELEASE_NOTES.md (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* [Infrastructure] Updated SDK to 3.1.301 (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* [Infrastructure] Added GitHub action to publish new version of Saturn (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* [Infrastructure] Update FAKE build script (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

### Removed

* Removed support for `netstandard2.0` (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Removed any obsolete APIs (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Remove deprecated `OpenIdConnect` extension (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

## [0.13.3] - 15.06.2020

### Changed
* Fix initialization error caused by `use_gzip` (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

## [0.13.2] - 11.06.2020

### Changed
* Make RequestUrl fetching lazy (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Added a few more mime-types that should be compressed. (by [@Thorium](https://github.com/Thorium))

## [0.13.1] - 27.04.2020

### Changed
* Make SocketMiddleware ~great~ public again (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

## [0.13.0] - 24.04.2020

### Changed
* Allow all functions in a channel to see the socketId (by [@robertpi](https://github.com/robertpi))
* Add exception handler for site.map generation (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Fix edit action in controller (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Moves `use_open_id_auth_with_config` to the Saturn.Extensions.Authorization project, obsoletes the old member and forwards it to the new project. (by [@ChrSteinert](https://github.com/ChrSteinert)))
* Channel improvements (typed `handle` action) (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Doc generation and infrastructure updates (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

## [0.12.1] - 18.02.2020

### Changed
* Add support for AzureAD OAuth (by [@ChrSteinert](https://github.com/ChrSteinert)))

## [0.12.0] - 18.02.2020

### Changed
* bump TFM to netcoreapp3.1 (by [@baronfel](https://github.com/baronfel))
* fully-qualify applicationbuilder type to use the saturn type instead of aspnetcore (by [@baronfel](https://github.com/baronfel))
* Fixed naming collision in Auth extensions (by [@rusanov-vladimir](https://github.com/rusanov-vladimir) )

## [0.10.1] - 25.11.2019

### Changed
* Update Giraffe to 4.0.1 (by [@mastoj](https://github.com/mastoj))

## [0.10.0] - 17.10.2019

### Changed
* add OpenId Saturn extension (by [@gfritz](https://github.com/gfritz))
* updated Giraffe to version 4 (by [@brase](https://github.com/brase))
* fix for signature of tryMatchInput (by [@brase](https://github.com/brase))
* renames AddAuthorization to AddAuthorizationCore (by [@brase](https://github.com/brase))
* Add more constraints for package dependencies (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

## [0.9.0 ]- 26.07.2019

### Changed
* Change `use_config` to accept `IConfiguration -> 'a` (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Case Insensitive support (`case_insensitive`) (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* OAuth refactoring (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Add overloads for controller actions that automatically providing dependencies (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Controller key should always match up to a single path segment  (by [@NinoFloris](https://github.com/NinoFloris))
* Add Chanel abstraction (websocket abstraction) (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak) and [@baronfel](https://github.com/baronfel))
* Add a tryCsrf handler as well as the CSRF handler (by [@baronfel](https://github.com/baronfel))
* Add routing diagnostic page (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Don't use setBody on CORS 204 (by [@Titaye](https://github.com/Titaye))
* Fix error on invalid key type if Patch is used (by [@Frassle](https://github.com/Frassle))
* Controller plug All should affect Patch (by [@Frassle](https://github.com/Frassle))
* Server channels design - SocketHub (by [@baronfel](https://github.com/baronfel))
* Make application working without router and add `no_router` operation (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Add `use_developer_exceptions` and `listen_local` to application CE (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Add gRPC extension (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Update Saturn to Asp.Net Core 2.2 (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

## [0.8.0 ]- 05.12.2018

### Changed
* Updates Index and DeleteAll to not result in plugs fired twice. (by [@jeremyabbott](https://github.com/jeremyabbott))
* Upgrade of Giraffe to 3.4, fixes breaking compilation errors. (by [@NinoFloris](https://github.com/NinoFloris))
* Fixes other spots that are related to #143, DeleteAll and Index, brings consistency, all plugs are now only run after route check (by [@NinoFloris](https://github.com/NinoFloris))
* Removes stringConvert, we now completely rely on giraffe to convert our route segments, which means ShortID and ShortGuid automatically work as they should, also adds quite some tests for (sub) routing (by [@NinoFloris](https://github.com/NinoFloris))

## [0.7.6 ]- 05.11.2018

### Changed
* Add extension for turbolinks
* Fix turbolinks header application (by [@NinoFloris](https://github.com/NinoFloris))
* Set 404 status code on not found handlers in sample
* return an IActionResult over HttpResponse in azure functions handler
* Fix for plugs firing twice (by [@jeremyabbott](https://github.com/jeremyabbott))


## [0.7.5 ]- 03.08.2018

### Changed
* Check state.Urls before running UseUrls (by [@NinoFloris](https://github.com/NinoFloris))
* fixes missing doctype in html controller actions (by [@WalternativE](https://github.com/WalternativE ))
* Add abstraction for hosting Saturn on Azure Functions (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Enable packaging for extensions (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

## [0.7.4 ]- 20.07.2018

### Changed
* Fix adding multiple auth sources (by [@BohdanZhmud](https://github.com/BohdanZhmud))
* Fix controller nesting bug (by [@TWith2Sugars](https://github.com/TWith2Sugars))
* Reworks 'Key to string conversion as just using ToString was not the best way to tackle the SRTP constraint, `string` decides per type to do invariant conversions and format specializations if needed (by [@NinoFloris](https://github.com/NinoFloris))
* Add protection against subcontroller routes that don't start with a forward slash, which lead to unwanted behavior (by [@NinoFloris](https://github.com/NinoFloris))
* Use fake 5 api (by [@jeremyabbott](https://github.com/jeremyabbott))

## [0.7.3 ]- 14.07.2018

### Changed
* Use earlier FSharp.Core version (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Remove unnecessary ObsoleteAttribute from `use_router` (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

## [0.7.0 ]- 13.07.2018

### Changed
* Fix GitHub OAuth (by [@rmunn](https://github.com/rmunn))
* Rename `scope` to `router` (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Fix `jsonToClaimMap` in GitHub OAuth (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Remove ContentRoot call from `use_static` (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Fix `site.map` generation if no routes were detected (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Add generic `response` helper (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Handle generic output type in controller actions (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Removes patch request method from `Update` into a new action called `Patch` (by [@NinoFloris](https://github.com/NinoFloris))
* Adds HttpSys project and sample (by [@ChrSteinert](https://github.com/ChrSteinert)))
* Add Google OAuth (by [@rmunn](https://github.com/rmunn))
* Move OAuth authorization with 3rd party services to extensions library (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Add operations for customizing serialization (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* New `except` function taking actions to remove from the complete set of actions (by [@NinoFloris](https://github.com/NinoFloris))
* Handle possible `except [All]` silliness (by [@rmunn](https://github.com/rmunn))
* Make `version` in `controller` CE a string (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Ensure cookies are enabled only once (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Add `use_router` to application CE. Mark `router` as obsolete. (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Support custom MIME types (by [@Nhowka](https://github.com/Nhowka))
* Fix model loading (`fetchModel`, `loadModel` and `getModel` functions) (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

## [0.6.0 ]- 03.07.2018

### Changed
* Implement CSRF token protection using `Microsoft.AspNetCore.Antiforgery` (by @baronfel)
* Await before adding result to `Items.["RequestModel"]` (by @NinoFloris)
* Make IDs with # in them URL-quoted (by @NinoFloris)
* Fix unintuitive ordering of plugs (by @NinoFloris)
* Replace tupled controller args with curried args (by @rusanov-vladimir)
* Add `cli_arguments` operation to the Application CE to
flow into `CreateDefaultBuilder(args)` (by @NinoFloris)

## [0.5.0 ]- 22.05.2018

### Changed
* Authorization helper (by @Nhowka)
* Add forwardf
* Generate documentation XML file (by @alfonsogarciacaro)
* Fixed Controller DELETE to prevent NRE (by @rusanov-vladimir)
* Update Application.fs - `use_static` changes (by @isaacabraham)
* expose IWebHostBuilder from application CE (by @mexx)
* Fix bug with `delete` routing (by @WalternativE)
* Allow for creation of controller without typed actions (by @jeremyabbott)
* Implementation of site map generator
* Fix `set_body` overload

## [0.4.3 ]- 27.02.2018

### Changed
* Update to Giraffe 1.1
* Add new renderXml to render XmlNode based templates  (by @mtnrbq)

## [0.4.2 ]- 19.02.2018

### Changed
* Fix `create` and `delete` actions
* Add `delete_all` action

## [0.4.0 ]- 16.02.2018

### Changed
* Implement controller versioning
* Add suport for embedding controllers
* Add plugs per action to controller
* Add application helper for enabling IIS integration
* Refactor authentication
* Add cookies auth helpers
* Add keyword for custom service configuration step
* Add error handler to controller
* Add helper for configuring logging
* Add application helpers for OAuth and GitHub OAuth
* Update to Giraffe 1.0
* Add AutoOpens to most modules

## [0.3.1 ]- 03.02.2018

### Changed
* Fix JWT authorization
* Set content root in `use_static`

## [0.3.0 ]- 02.02.2018

### Changed
* Add toggle for forcing SSL
* Add toggle for forcing CORS
* Add helpers for JWT authentication

## [0.2.0 ]- 25.01.2018

### Changed

* Initial version
* Implemented `pipeline` abstraction
* Implemented `scope` abstraction
* Implemented `controller` abstraction
* Implemented `application` abstraction
* Implemented set of helpers for controllers
* Implemented set of helpers for generating links following controller conventions
* Implemented CORS handler
