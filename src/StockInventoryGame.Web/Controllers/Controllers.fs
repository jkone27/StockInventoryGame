namespace StockInventoryGame.Web

open Microsoft.AspNetCore.Http

module HttpHelpers =
    open StockInventoryGame.Services
    open Giraffe

    let resultToHttp (fallback : HttpHandler) (responseObject: string -> HttpHandler) result =
        match result with
        |Ok(o) ->
            match o with
            |Completed -> 
                fallback
            |NoOperation ->
                Successful.NO_CONTENT
            |Success.Response(json) ->
                responseObject(json)
        |Error(e) ->
            match e with
            |ErrorResult.NotFound ->
                RequestErrors.NOT_FOUND("not found")
            |ErrorResult.ValidationError(err) ->
                RequestErrors.BAD_REQUEST(err)
            |ErrorResult.UnrecoverableError(e) ->
                ServerErrors.INTERNAL_ERROR(e)

module Controllers =
    
    open HttpHelpers
    open Giraffe
    open StockInventoryGame.Services
    open FSharp.Control.Tasks.V2
    open StockInventoryGame.Dtos
    open FSharp.Data

    let getArticles : HttpHandler = 
        fun next ctx -> task {
            return! json (Services.getArticles().JsonValue.ToString()) next ctx
            }

    let postArticles : HttpHandler =
        fun next ctx -> 
            task { 
                let! body = ctx.ReadBodyFromRequestAsync()
                let requestDto = 
                    body
                    |> JsonValue.Parse
                    |> Dtos.Inventory

                let jsonResponse = //todo should return new inserted ids, requires data changes
                    json requestDto
                
                let responseHandler = 
                    Services.addArticles(requestDto) 
                    |> resultToHttp jsonResponse (fun o -> json o)

                return! (responseHandler next ctx)
            }

    let articlesController : HttpHandler = 
        route "/articles" >=> choose [
          GET  >=> getArticles
          POST >=> postArticles
        ]

    let getProducts : HttpHandler =
        fun next ctx -> task {
            return! json (Services.getProducts().JsonValue.ToString()) next ctx
        }

    let postProducts : HttpHandler =
        fun next ctx -> 
            task { 
                let! body = ctx.ReadBodyFromRequestAsync()
                let requestDto = 
                    body
                    |> JsonValue.Parse
                    |> Dtos.Products

                let jsonResponse = //todo should return new inserted ids, requires data changes
                    json requestDto
                
                let responseHandler = 
                    Services.addProducts(requestDto) 
                    |> resultToHttp jsonResponse (fun o -> json o)

                return! responseHandler next ctx
            }

    let sellProducts : HttpHandler =
        fun next ctx -> 
            task { 

                let! body = ctx.ReadBodyFromRequestAsync()
                let requestDto = 
                    body
                    |> JsonValue.Parse
                    |> Dtos.Product

                let jsonResponse = //todo should return new inserted ids, requires data changes
                    json requestDto

                let responseHandler = 
                    Services.sellProduct(requestDto) 
                    |> resultToHttp jsonResponse (fun o -> json o)

                return! responseHandler next ctx
            }
            
    let productsController : HttpHandler = 
        route "/products" >=> choose [
          GET  >=> getProducts
          POST >=> postProducts
          route "/sell" >=> POST >=> sellProducts
        ]
