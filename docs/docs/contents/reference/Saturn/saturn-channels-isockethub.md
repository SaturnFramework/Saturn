---
title: Saturn | ISocketHub
layout: standard
---

# ISocketHub

**Namespace:** [Saturn](./saturn.html)

**Parent:** [Channels](./saturn-channels.html)

Interface representing server side Socket Hub, giving you ability to brodcast messages (either to particular socket or to all sockets). You can get instance of it with `ctx.GetService&lt;Saturn.Channels.ISocketHub>()` from any place that has access to HttpContext instance (`controller` actions, `channel` actions, normal `HttpHandler`).

| Name                                        | Description | Implementation Link                                                                         |
|---------------------------------------------|-------------|---------------------------------------------------------------------------------------------|
| Instance Members                            |             |                                                                                             |
| `x.SendMessageToClient arg1 arg2 arg3 arg4` |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Channels.fs#L56-56) |
| `x.SendMessageToClients arg1 arg2 arg3`     |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Channels.fs#L55-55) |
