namespace Shared

open System.ServiceModel
open System.Threading.Tasks
open System.Runtime.Serialization

[<DataContract; CLIMutable>]
type MultiplyRequest =
  { [<DataMember(Order = 1)>]
    X: int
    [<DataMember(Order = 2)>]
    Y: int }

[<DataContract; CLIMutable>]
type MultiplyResult =
  { [<DataMember(Order = 1)>]
    Result: int }

[<DataContract; CLIMutable>]
type HelloRequest =
  { [<DataMember(Order = 1)>]
    Parameter: string }

[<DataContract; CLIMutable>]
type HelloResult =
  { [<DataMember(Order = 1)>]
    Response: string }

[<ServiceContract(Name = "Hyper.Calculator")>]
type ICalculator =
  abstract MultiplyAsync : MultiplyRequest -> ValueTask<MultiplyResult>

[<ServiceContract(Name = "Hello.Test")>]
type IHello =
  abstract TestAsync : HelloRequest -> ValueTask<HelloResult>
