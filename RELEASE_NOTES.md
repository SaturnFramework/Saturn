### 0.7.1 - 14.07.2018
* Use earlier FSharp.Core version (by [@Krzysztof-Cieslak](https://github.com/Krzysztof-Cieslak))

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