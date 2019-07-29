### 0.9.1-dev - 26.07.2019
* Update dependencies ranges in `paket.dependencies` file (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

### 0.9.0 - 26.07.2019
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

### 0.8.0 - 05.12.2018
* Updates Index and DeleteAll to not result in plugs fired twice. (by [@jeremyabbott](https://github.com/jeremyabbott))
* Upgrade of Giraffe to 3.4, fixes breaking compilation errors. (by [@NinoFloris](https://github.com/NinoFloris))
* Fixes other spots that are related to #143, DeleteAll and Index, brings consistency, all plugs are now only run after route check (by [@NinoFloris](https://github.com/NinoFloris))
* Removes stringConvert, we now completely rely on giraffe to convert our route segments, which means ShortID and ShortGuid automatically work as they should, also adds quite some tests for (sub) routing (by [@NinoFloris](https://github.com/NinoFloris))

### 0.7.6 - 05.11.2018
* Add extension for turbolinks
* Fix turbolinks header application (by [@NinoFloris](https://github.com/NinoFloris))
* Set 404 status code on not found handlers in sample
* return an IActionResult over HttpResponse in azure functions handler
* Fix for plugs firing twice (by [@jeremyabbott](https://github.com/jeremyabbott))


### 0.7.5 - 03.08.2018
* Check state.Urls before running UseUrls (by [@NinoFloris](https://github.com/NinoFloris))
* fixes missing doctype in html controller actions (by [@WalternativE](https://github.com/WalternativE ))
* Add abstraction for hosting Saturn on Azure Functions (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Enable packaging for extensions (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

### 0.7.4 - 20.07.2018
* Fix adding multiple auth sources (by [@BohdanZhmud](https://github.com/BohdanZhmud))
* Fix controller nesting bug (by [@TWith2Sugars](https://github.com/TWith2Sugars))
* Reworks 'Key to string conversion as just using ToString was not the best way to tackle the SRTP constraint, `string` decides per type to do invariant conversions and format specializations if needed (by [@NinoFloris](https://github.com/NinoFloris))
* Add protection against subcontroller routes that don't start with a forward slash, which lead to unwanted behavior (by [@NinoFloris](https://github.com/NinoFloris))
* Use fake 5 api (by [@jeremyabbott](https://github.com/jeremyabbott))

### 0.7.3 - 14.07.2018
* Use earlier FSharp.Core version (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))
* Remove unnecessary ObsoleteAttribute from `use_router` (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

### 0.7.0 - 13.07.2018
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

### 0.6.0 - 03.07.2018
* Implement CSRF token protection using `Microsoft.AspNetCore.Antiforgery` (by @baronfel)
* Await before adding result to `Items.["RequestModel"]` (by @NinoFloris)
* Make IDs with # in them URL-quoted (by @NinoFloris)
* Fix unintuitive ordering of plugs (by @NinoFloris)
* Replace tupled controller args with curried args (by @rusanov-vladimir)
* Add `cli_arguments` operation to the Application CE to
flow into `CreateDefaultBuilder(args)` (by @NinoFloris)

### 0.5.0 - 22.05.2018
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

### 0.4.3 - 27.02.2018
* Update to Giraffe 1.1
* Add new renderXml to render XmlNode based templates  (by @mtnrbq)

### 0.4.2 - 19.02.2018
* Fix `create` and `delete` actions
* Add `delete_all` action

### 0.4.0 - 16.02.2018
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

### 0.3.1 - 03.02.2018
* Fix JWT authorization
* Set content root in `use_static`

### 0.3.0 - 02.02.2018
* Add toggle for forcing SSL
* Add toggle for forcing CORS
* Add helpers for JWT authentication

### 0.2.0 - 25.01.2018

* Initial version
* Implemented `pipeline` abstraction
* Implemented `scope` abstraction
* Implemented `controller` abstraction
* Implemented `application` abstraction
* Implemented set of helpers for controllers
* Implemented set of helpers for generating links following controller conventions
* Implemented CORS handler