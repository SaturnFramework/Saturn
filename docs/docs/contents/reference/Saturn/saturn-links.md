---
title: Saturn | Links
layout: standard
---

# Links

**Namespace:** [Saturn](./saturn.html)

**Parent:** [Saturn](./saturn.html)

Convention-based links to other actions to perform on the current request model.

**Values and Functions**

| Name            | Description                                                                                              | Implementation Link                                                                      |
|-----------------|----------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------|
| `index(ctx)`    | Returns a link to the `index` action for the current model.                                              | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Links.fs#L9-9)   |
| `add(ctx)`      | Returns a link to the `add` action for the current model.                                                | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Links.fs#L20-20) |
| `withId ctx id` | Returns a link to the `withId` action for a particular resource of the same type as the current request. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Links.fs#L24-24) |
| `edit ctx id`   | Returns a link to the `edit` action for a particular resource of the same type as the current request.   | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Links.fs#L28-28) |
