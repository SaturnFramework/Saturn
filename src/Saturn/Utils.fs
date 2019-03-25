namespace Saturn

module Utils =
  open System.Linq
  open Microsoft.Extensions.DependencyInjection

  module String =
    let internal equalsCaseInsensitive (a : string) (b : string) =
      a.Equals(b, System.StringComparison.InvariantCultureIgnoreCase)


  module DependencyInjection =

    let (|TypeImpl|InstanceImpl|FactoryImpl|) (reg: ServiceDescriptor) =
      if reg.ImplementationType <> null then TypeImpl(reg.ServiceType, reg.ImplementationType)
      else if reg.ImplementationInstance <> null then InstanceImpl (reg.ServiceType, reg.ImplementationInstance)
      else FactoryImpl (reg.ServiceType, reg.ImplementationFactory) // factories are of the type 'deps -> 'impl, and we want 'impl

    type IServiceCollection with
      /// register the last service as itself too
      /// TODO: add in more cases as necessary.
      ///
      /// The first case necessary was to register a type that was registered as an interface as the base type too, with the same lifetime
      member x.AsSelf() =
        match x.LastOrDefault() with
        | null -> ()
        | TypeImpl (serviceTy, implTy) as reg ->
          if serviceTy <> implTy
          then
            // printfn "adding %s as implementation of itself" (implTy.FullName)
            x.Add(ServiceDescriptor(implTy, implTy, reg.Lifetime))
          else
            // printfn "types %s and %s were the same" serviceTy.FullName implTy.FullName
            ()
        | InstanceImpl (serviceTy, impl) ->
          // printfn "instance impl of %s using %s" serviceTy.FullName (impl.GetType().FullName)
          ()
        | FactoryImpl (serviceTy, factory) ->
          // printfn "factory impl of %s using factory func of type `IServiceProvider -> %s`" serviceTy.FullName (factory.GetType().GenericTypeArguments.[1].FullName)
          ()
        x




