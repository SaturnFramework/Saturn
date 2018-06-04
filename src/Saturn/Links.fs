namespace Saturn

open Microsoft.AspNetCore.Http

///Convention-based links to other actions to perform on the current request model.
module Links =
  ///Returns a link to the `index` action for the current model.
  let index (ctx : HttpContext) =
    let v = ctx.Request.Path.Value.Split('/')
    let res =
      match Array.last v with
      | "" -> v
      | "edit" -> [| yield! v.[0 .. v.Length - 3 ]; yield "" |] //Remove 2 last parts of the path
      | _ -> [| yield! v.[0 .. v.Length - 2]; yield "" |] //Remove the last part of the path
      |> String.concat "/"
    res

  ///Returns a link to the `add` action for the current model.
  let add (ctx: HttpContext) =
    index ctx + "add"

  ///Returns a link to the `withId` action for a particular resource of the same type as the current request.
  let withId (ctx: HttpContext) id =
    index ctx + id

  ///Returns a link to the `edit` action for a particular resource of the same type as the current request.
  let edit (ctx: HttpContext) id =
    index ctx + id + "/edit"
