---
title: Saturn | Response
layout: standard
---

# Response

**Namespace:** [Saturn](./saturn.html)

**Parent:** [ControllerHelpers](./saturn-controllerhelpers.html)

This module wraps Giraffe responses (ie setting HTTP status codes) for easy chaining in the Saturn model. All of the functions set the status code and halt further processing.

**Values and Functions**

| Name                                | Description | Implementation Link                                                                                    |
|-------------------------------------|-------------|--------------------------------------------------------------------------------------------------------|
| `continue(ctx)`                     |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L187-187) |
| `switchingProto(ctx)`               |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L190-190) |
| `ok ctx res`                        |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L193-193) |
| `created ctx res`                   |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L196-196) |
| `accepted ctx res`                  |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L199-199) |
| `badRequest ctx res`                |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L202-202) |
| `unauthorized ctx scheme realm res` |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L205-205) |
| `forbidden ctx res`                 |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L208-208) |
| `notFound ctx res`                  |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L211-211) |
| `methodNotAllowed ctx res`          |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L214-214) |
| `notAcceptable ctx res`             |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L217-217) |
| `conflict ctx res`                  |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L220-220) |
| `gone ctx res`                      |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L223-223) |
| `unuspportedMediaType ctx res`      |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L226-226) |
| `unprocessableEntity ctx res`       |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L229-229) |
| `preconditionRequired ctx res`      |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L232-232) |
| `tooManyRequests ctx res`           |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L235-235) |
| `internalError ctx res`             |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L238-238) |
| `notImplemented ctx res`            |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L241-241) |
| `badGateway ctx res`                |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L244-244) |
| `serviceUnavailable ctx res`        |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L247-247) |
| `gatewayTimeout ctx res`            |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerHelpers.fs#L250-250) |
