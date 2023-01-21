---
title: Saturn | ControllerHelpers
layout: standard
---

# Controller

**Namespace:** [Saturn](./saturn.html)

**Parent:** [ControllerHelpers](./saturn-controllerhelpers.html)

Module containing helpers for `controller` actions.

**Values and Functions**

| Name                             | Description                                                                                                                         | Implementation Link                                                                                    |
|----------------------------------|-------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------|
| `json ctx obj`                   | Returns to the client content serialized to JSON.                                                                                   | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L21-21)   |
| `jsonCustom ctx settings obj`    | Returns to the client content serialized to JSON. Accepts custom serialization settings.                                            | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L25-25)   |
| `xml ctx obj`                    | Returns to the client content serialized to XML.                                                                                    | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L29-29)   |
| `text ctx value`                 | Returns to the client content as string.                                                                                            | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L33-33)   |
| `html ctx template`              | Returns the string template as html to the client.                                                                                  | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L37-37)   |
| `renderHtml ctx template`        | Returns to the client rendered html template.                                                                                       | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L42-42)   |
| `file ctx path`                  | Returns to the client static file.                                                                                                  | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L46-46)   |
| `response ctx output`            | Returns to the client response according to accepted content type (`Accept` header, and if it's not present `Content-Type` header). | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L50-50)   |
| `getJson(ctx)`                   | Gets model from body as JSON.                                                                                                       | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L108-108) |
| `getXml(ctx)`                    | Gets model from body as XML.                                                                                                        | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L112-112) |
| `getForm(ctx)`                   | Gets model from url-encoded body.                                                                                                   | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L116-116) |
| `getFormCulture ctx culture`     | Gets model from urelencoded body. Accepts culture name                                                                              | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L120-120) |
| `getQuery(ctx)`                  | Gets model from query string.                                                                                                       | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L125-125) |
| `getQueryCulture ctx culture`    | Gets model from query string. Accepts culture name.                                                                                 | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L129-129) |
| `getModel(ctx)`                  | Get model based on `HttpMethod` and `Content-Type` of request.                                                                      | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L134-134) |
| `getModelCustom ctx culture`     | Get model based on `HttpMethod` and `Content-Type` of request. Accepts custom culture.                                              | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L141-141) |
| `loadModel(ctx)`                 | Loads model populated by `fetchModel` pipeline.                                                                                     | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L148-148) |
| `getPath(ctx)`                   | Gets path of the request - it's relative to current `scope`.                                                                        | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L155-155) |
| `getUrl(ctx)`                    | Gets url of the request.                                                                                                            | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L159-159) |
| `getConfig(ctx)`                 | Gets the contents of the `Configuration` key in the HttpContext dictionary, unboxed as the given type.                              | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L168-168) |
| `sendDownload ctx path`          | Sends the contents of a file as the body of the response. Does not set a `Content-Type`.                                            | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L172-172) |
| `sendDownloadBinary ctx content` | Send bytes as the body of the response. Does not set a `Content-Type`.                                                              | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L177-177) |
| `redirect ctx path`              | Perform a temporary redirect to the provided location.                                                                              | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L181-181) |
