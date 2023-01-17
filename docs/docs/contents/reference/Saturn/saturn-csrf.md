---
title: Saturn | CSRF
layout: standard
---

# CSRF

**Namespace:** [Saturn](./saturn.html)

**Parent:** [Saturn](./saturn.html)

Module containing helpers for CSRF Antiforgery protection.

**Declared Types**

| Type                                      | Description |
|-------------------------------------------|-------------|
| [CSRFError](./saturn-csrf-csrferror.html) |             |

**Declared Modules**

| Module                          | Description                                                     |
|---------------------------------|-----------------------------------------------------------------|
| [View](./saturn-csrf-view.html) | Contains view helpers for csrf tokens for various view engines. |

**Values and Functions**

| Name                            | Description                                                                                                                                                                                                                                                                                                                                                                         | Implementation Link                                                                     |
|---------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------|
| `tryCsrf errorHandler next ctx` | Protect a resource by validating that requests that can change state come with a valid request antiforgery token, which is based off of a known session token. The particular configuration options can be set via the `application` builder's `use_antiforgery_with_config` method. If the request is not valid, a custom error handler will be invoked with the validation error. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/CSRF.fs#L61-61) |
| `csrf`                          | Protect a resource by validating that requests that can change state come with a valid request antiforgery token, which is based off of a known session token. The particular configuration options can be set via the `application` builder's `use_antiforgery_with_config` method.                                                                                                | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/CSRF.fs#L74-74) |
| `getRequestTokens(ctx)`         |                                                                                                                                                                                                                                                                                                                                                                                     | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/CSRF.fs#L97-97) |

**Type Extensions**

| Name                  | Description                                                                                                                                                                                                                                                                                                                                                      | Implementation Link                                                                     |
|-----------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------|
| `x.ValidateCSRF()`    | Protect a resource by validating that requests that can change state come with a valid request antiforgery token, which is based off of a known session token. The particular configuration options can be set via the `application` builder's `use_antiforgery_with_config` method. If the request is not valid, an exception will be thrown with details.      | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/CSRF.fs#L81-81) |
| `x.TryValidateCSRF()` | Protect a resource by validating that requests that can change state come with a valid request antiforgery token, which is based off of a known session token. The particular configuration options can be set via the `application` builder's `use_antiforgery_with_config` method. If the request is not valid, an Error result will be returned with details. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/CSRF.fs#L94-94) |
