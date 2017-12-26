namespace Saturn

[<AutoOpen>]
module Common =

  [<RequireQualifiedAccess>]
  type InclusiveOption<'T> =
  | None
  | Some of 'T
  | All

  let inline internal succeed nxt cntx  = nxt cntx