---
title: Saturn | JoinResult
layout: standard
---

# JoinResult

**Namespace:** [Saturn](./saturn.html)

**Parent:** [Channels](./saturn-channels.html)

Type representing result of `join` action. It can be either succesful (`Ok`) or you can reject client connection (`Rejected`).

| Name               | Description | Implementation Link                                                                         |
|--------------------|-------------|---------------------------------------------------------------------------------------------|
| Union Cases        |             |                                                                                             |
| `Ok`               |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Channels.fs#L42-42) |
| `Rejected(reason)` |             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Channels.fs#L43-43) |
