namespace Saturn

module Utils =
  open System.Linq
  open Microsoft.Extensions.DependencyInjection

  module String =
    let internal equalsCaseInsensitive (a : string) (b : string) =
      a.Equals(b, System.StringComparison.InvariantCultureIgnoreCase)
