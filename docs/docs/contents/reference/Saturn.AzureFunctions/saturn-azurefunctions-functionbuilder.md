---
title: Saturn | FunctionBuilder
layout: standard
---

# FunctionBuilder

**Namespace:** [Saturn](./saturn.html)

**Parent:** [AzureFunctions](./saturn-azurefunctions.html)

| Name                                           | CE Custom Operation      | Description                                                                                                   | Implementation Link                                                                                                |
|------------------------------------------------|--------------------------|---------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------|
| `x.ConfigJSONSerializer(state, settings)`      | `use_json_settings`      | Configures built in JSON.Net (de)serializer with custom settings.                                             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.AzureFunctions/AzureFunctions.fs#L110-110) |
| `x.ConfigXMLSerializer(state, settings)`       | `use_xml_settings`       | Configures built in XML (de)serializer with custom settings.                                                  | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.AzureFunctions/AzureFunctions.fs#L120-120) |
| `x.ErrorHandler(state, handler)`               | `error_handler`          | Adds error handler for the function.                                                                          | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.AzureFunctions/AzureFunctions.fs#L89-89)   |
| `x.HostPrefix(state, prefix)`                  | `host_prefix`            | Adds prefix for the endpoint. By default Azure Functions are using /api prefix.                               | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.AzureFunctions/AzureFunctions.fs#L105-105) |
| `x.Logger(state, logger)`                      | `logger`                 | Adds logger for the function. Used for error reporting and passed to the actions as ctx.Items.["TraceWriter"] | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.AzureFunctions/AzureFunctions.fs#L99-99)   |
| `x.NotFoundHandler(state, handler)`            | `not_found_handler`      | Adds not found handler for the function.                                                                      | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.AzureFunctions/AzureFunctions.fs#L94-94)   |
| `x.Router(state, handler)`                     | `use_router`             | Defines top-level router used for the function.                                                               | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.AzureFunctions/AzureFunctions.fs#L84-84)   |
| `x.Run(state)`                                 |                          |                                                                                                               | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.AzureFunctions/AzureFunctions.fs#L44-44)   |
| `x.UseConfigNegotiation(state, config)`        | `use_negotiation_config` | Configures negotiation config.                                                                                | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.AzureFunctions/AzureFunctions.fs#L130-130) |
| `x.UseCustomJSONSerializer(state, serializer)` | `use_json_serializer`    | Replaces built in JSON.Net (de)serializer with custom serializer.                                             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.AzureFunctions/AzureFunctions.fs#L115-115) |
| `x.UseCustomXMLSerializer(state, serializer)`  | `use_xml_serializer`     | Replaces built in XML (de)serializer with custom serializer.                                                  | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.AzureFunctions/AzureFunctions.fs#L125-125) |
| `x.Yield(arg1)`                                |                          |                                                                                                               | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.AzureFunctions/AzureFunctions.fs#L35-35)   |
| `x.LogWriter()`                                |                          |                                                                                                               | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.AzureFunctions/AzureFunctions.fs#L33-33)   |
| `x.LogWriter()`                                |                          |                                                                                                               | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.AzureFunctions/AzureFunctions.fs#L33-33)   |
