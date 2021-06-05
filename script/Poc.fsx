#r "nuget:Npgsql.FSharp"
#r "nuget:FSharp.Data"
#r "nuget:Suave"

open FSharp.Data
open FSharp.Core

module Constants =
    [<Literal>]
    let inventoryJson = __SOURCE_DIRECTORY__ + "/../inventory.json"

    [<Literal>]
    let productsJson = __SOURCE_DIRECTORY__ + "/../products.json"

    [<Literal>]
    let sqlDevConnection = "Host=localhost;Database=postgres;Username=postgres;Password=password"


//sensitive data should be injected from environment
System.Environment.SetEnvironmentVariable("DATABASE_CONNECTION_STRING", Constants.sqlDevConnection)

module ProvidedTypes =
    //open FSharp.Data.Npgsql
    type InventoryProvider = JsonProvider<Constants.inventoryJson>
    type ProductsProvider = JsonProvider<Constants.productsJson>
    //type NpgsqlProvider = NpgsqlConnection<Constants.sqlDevConnection>

module Dtos = 
    open ProvidedTypes

    type Inventory = InventoryProvider.Root
    type Article = InventoryProvider.Inventory
    type Products = ProductsProvider.Root
    type Product = ProductsProvider.Product
    type ArticleQuantity = ProductsProvider.ContainArticle

module Domain =
    type Article = { 
            Id : int ; 
            Name: string; 
            Stock: int 
        } 
        with member this.ToDto() = 
                Dtos.Article(this.Id, this.Name, this.Stock)

    type Article
        with static member FromDto(dto : Dtos.Article) =
                { Id = dto.ArtId; Name = dto.Name; Stock = dto.Stock } 

    type ArticleQuantity = {
            Id: int; 
            Qty: int 
        } 

    type Product = { 
            Id : int ; 
            Name: string; 
            Articles : ArticleQuantity list }
        with member this.ToDto() = 
                let articles = 
                    this.Articles 
                    |> Seq.map (fun a -> Dtos.ArticleQuantity(a.Id, a.Qty))
                    |> Array.ofSeq
                Dtos.Product(this.Name, articles)

module Data =
    open ProvidedTypes
    open Domain
    open Npgsql.FSharp

    module Queries =

        [<Literal>]
        let getArticles =
            "SELECT x \n\
            FROM test.articles"

        [<Literal>]
        let getProducts =
            "SELECT \n    \
                p.id, p.prd_name, a.art_name, pa.qty \n\
            FROM test.products as p \n\
            INNER JOIN test.products_articles as pa \n   \
                ON pa.product_id = p.id \n\
            INNER JOIN test.articles as a \n   \
                ON pa.article_id = a.id"

    let getConnectionString () =
        System.Environment.GetEnvironmentVariable "DATABASE_CONNECTION_STRING"

    let getArticles () =
        getConnectionString()
        |> Sql.connect
        |> Sql.query Queries.getArticles
        |> Sql.execute (fun read ->
            {
                Id = read.int "id"
                Name = read.text "art_name"
                Stock = read.int "stock"
            })

    let getProducts () : Product list =
         getConnectionString()
        |> Sql.connect
        |> Sql.query Queries.getProducts
        |> Sql.execute (fun read ->
            let id = read.int "id"
            { 
                    Id = id; 
                    Name =  read.text "prd_name"; 
                    Articles = [ {  
                            Id = read.int "id"; 
                            Qty = read.int "qty" 
                        } ]
            }    
        )
        |> List.groupBy (fun p -> p.Id)
        |> List.map (fun (pid,productLines) ->
            let product = productLines |> List.head

            { 
                product with Articles = 
                    productLines 
                    |> List.collect (fun p -> p.Articles ) 
            }
        )


open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

module Utils =

    let FromOption ser r =
        match r with
            |Some(o) -> OK(ser(o))
            |None -> RequestErrors.NOT_FOUND("not found")

    let FromList ser l =
        match l with
        |x::xs -> OK(ser(l))
        |[] -> RequestErrors.NOT_FOUND("not found")

    let FromRes fromWrapped result  = 
        match result with
        |Ok(r) -> 
            fromWrapped(r)
        |Error(e) -> ServerErrors.INTERNAL_ERROR(e)


module Services = 
    let getArticles (httpRequest : HttpRequest) = 
        Data.getArticles()
        //|> Async.RunSynchronously
        |> Seq.map (fun x -> x.ToDto())
        |> Seq.toArray
        |> fun x -> Dtos.Inventory(x)
        |> Some
        |> Ok
        |> Utils.FromRes(Utils.FromOption(fun x -> x.JsonValue.ToString()))

    let getProducts (httpRequest : HttpRequest) = 
        Data.getProducts()
        //|> Async.RunSynchronously
        |> Seq.map (fun x -> x.ToDto())
        |> Seq.toArray
        |> fun x -> Dtos.Products(x)
        |> Some
        |> Ok
        |> Utils.FromRes(Utils.FromOption(fun x -> x.JsonValue.ToString()))

    let sellProduct (httpRequest : HttpRequest) = 
        let productDto : Dtos.Products =
            httpRequest.rawForm
            |> System.Text.Encoding.UTF8.GetString
            |> ProvidedTypes.ProductsProvider.Parse

        Data.getProducts()
        //|> Async.RunSynchronously
        |> Seq.map (fun x -> x.ToDto())
        |> Seq.toArray
        |> fun x -> Dtos.Products(x)
        |> Some
        |> Ok
        |> Utils.FromRes(Utils.FromOption(fun x -> x.JsonValue.ToString()))

    let addArticles (httpRequest : HttpRequest) = 
        let newInventoryDto =
            httpRequest.rawForm
            |> System.Text.Encoding.UTF8.GetString
            |> ProvidedTypes.InventoryProvider.Parse
            
        let domainArticles = 
            newInventoryDto
            |> fun x -> x.Inventory
            |> Array.map (fun x -> Domain.Article.FromDto)
        
        //POST should replace all (idempotent?)
        newInventoryDto
        |> fun x -> x.JsonValue.ToString()
        |> OK

module Controllers = 
    let articlesController : WebPart = 
        path "/articles" >=> choose [
          GET  >=> request Services.getArticles
          POST >=> request Services.addArticles
        ]

    let productsController : WebPart = 
        path "/products" >=> choose [
          GET  >=> request  Services.getProducts
          POST >=> request (fun r -> ServerErrors.NOT_IMPLEMENTED("ouch"))
        ]

let app =
  choose [ 
      Controllers.articlesController 
      Controllers.productsController 
      RequestErrors.NOT_FOUND "Path was not found"
  ]

startWebServer defaultConfig app