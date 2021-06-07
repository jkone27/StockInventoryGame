namespace StockInventoryGame.IntegrationTests

open System
open Xunit
open JsonMutator
open FSharp.Control.Tasks.V2


type Tests(factory: TestApplicationFactory) =

    do ()
    interface IClassFixture<TestApplicationFactory>

    [<Fact>]
    member this.``Up and Running`` () =
        task {
            let client = factory.CreateClient()
            
            let! httpResponse = client.GetAsync("/products")

            httpResponse.EnsureSuccessStatusCode()
            |> ignore

            let! httpResponse = client.GetAsync("/articles")
            
            httpResponse.EnsureSuccessStatusCode()
            |> ignore
        }

    [<Fact>]
    member this.``Add inventory and Get inventory`` () =
        Assert.True(false)

    [<Fact>]
    member this.``Add products`` () =
        Assert.True(false)

    [<Fact>]
    member this.``Get all products and quantity of each that is an available with the current inventory (OK)`` () =
        Assert.True(false)

    [<Fact>]
    member this.``Remove(Sell) a product and update the inventory accordingly`` () =
        Assert.True(false)
