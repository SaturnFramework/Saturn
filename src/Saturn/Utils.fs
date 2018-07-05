namespace Saturn

module Utils =

  module String =
    let internal equalsCaseInsensitive (a : string) (b : string) =
      a.Equals(b, System.StringComparison.InvariantCultureIgnoreCase)
