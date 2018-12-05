module LinksTests

open Expecto
open Saturn
open Helpers

type LinkType =
  | Index
  | Add
  | WithId
  | Edit
  with
    override x.ToString() =
      match x with
      | Index -> "Index"
      | Add -> "Add"
      | WithId -> "WithId"
      | Edit -> "Edit"

let getLink typ ctx =
  match typ with
  | Index -> Links.index ctx
  | Add -> Links.add ctx
  | _ -> ""
  |> Controller.text ctx

let getLinkWithId typ ctx (id: string) =
  match typ with
  | Index -> Links.index ctx
  | Add -> Links.add ctx
  | WithId -> Links.withId ctx id
  | Edit -> Links.edit ctx id
  |> Controller.text ctx

let testController typ = controller {
  index (getLink typ)
  show (getLinkWithId typ)
  add (getLink typ)
  edit (getLinkWithId typ)
  create (getLink typ)
  update (getLinkWithId typ)
  patch (getLinkWithId typ)
  delete (getLinkWithId typ)
  delete_all (getLink typ)
}

let getResponse typ id =
  match typ, id with
  | Index, _ -> "/"
  | Add, _ -> "/add"
  | WithId, None -> ""
  | WithId, Some id -> "/" + id
  | Edit, None -> ""
  | Edit, Some id -> "/" + id + "/edit"

[<Tests>]
let linksTests =
    [Index; Add; WithId; Edit]
    |> List.map (fun typ ->
      let ctrl = testController typ
      let responseTestCase = responseTestCase ctrl
      let response = getResponse typ
      let desc = sprintf "Link tests for link type - %s" (typ.ToString())
      testList desc [
        testCase "index" <| responseTestCase "GET" "/" (response None)
        testCase "show" <| responseTestCase "GET" "/1" (response (Some "1"))
        testCase "add" <| responseTestCase "GET" "/add" (response None)
        testCase "edit" <| responseTestCase "GET" "/1/edit" (response (Some "1"))
        testCase "create" <| responseTestCase "POST" "/" (response None)
        testCase "update" <| responseTestCase "POST" "/1" (response (Some "1"))
        testCase "patch" <| responseTestCase "PATCH" "/1" (response (Some "1"))
        testCase "delete" <| responseTestCase "DELETE" "/1" (response (Some "1"))
        testCase "delete all" <| responseTestCase "DELETE" "/" (response None)
      ]
    )
  |> testList "Links tests"