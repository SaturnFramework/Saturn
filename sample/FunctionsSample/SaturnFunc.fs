namespace MyFunctions

open Microsoft.AspNetCore.Http
open Microsoft.Azure.WebJobs.Host
open Saturn.AzureFunctions
open Microsoft.Extensions.Logging

module Functions =
    open Saturn
    open Microsoft.Azure.WebJobs

    let testCntl = controller {
        index (fun ctx -> Controller.text ctx "Hello world")
        show (fun ctx id -> id |> sprintf "Hello world, %s" |> Controller.text ctx)
    }

    let func log = azureFunction {
        host_prefix "/api"
        use_router testCntl
        logger log
    }

    [<FunctionName("HelloWorld")>]
    let helloWorld ([<HttpTrigger(Extensions.Http.AuthorizationLevel.Anonymous, Route = "{route?}")>]req: HttpRequest, log: ILogger) =
        func log req
