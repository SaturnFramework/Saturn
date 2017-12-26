module Utils

module String =

  let equalsCaseInsensitive (a : string) (b : string) =
    a.Equals(b, System.StringComparison.InvariantCultureIgnoreCase)
