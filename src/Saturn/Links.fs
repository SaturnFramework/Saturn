namespace Saturn

open Microsoft.AspNetCore.Http

module Links =
  let index (ctx : HttpContext) =
    let v = ctx.Request.Path.Value.Split('/')
    let res =
      match Array.last v with
      | "" -> v
      | "edit" -> [| yield! v.[0 .. v.Length - 3 ]; yield "" |] //Remove 2 last parts of the path
      | _ -> [| yield! v.[0 .. v.Length - 2]; yield "" |] //Remove the last part of the path
      |> String.concat "/"
    res

  let add (ctx: HttpContext) =
    index ctx + "add"

  let withId (ctx: HttpContext) id =
    index ctx + id

  let edit (ctx: HttpContext) id =
    index ctx + id + "/edit"
