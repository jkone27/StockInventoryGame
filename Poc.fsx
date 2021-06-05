open System.Data
open System.Data.Common
#r "nuget:Npgsql"
#r "nuget:Npgsql.FSharp"
#r "nuget:FSharp.Data"
#r "nuget:Dapper.FSharp"
#r "nuget:Suave"

open FSharp.Data
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Npgsql.FSharp
open System.ComponentModel.DataAnnotations.Schema

module Constants =
    [<Literal>]
    let inventoryJson = __SOURCE_DIRECTORY__ + "/inventory.json"

    [<Literal>]
    let productsJson = __SOURCE_DIRECTORY__ + "/products.json"

    [<Literal>]
    let sqlDevConnection = "Host=localhost;Database=postgres;Username=postgres;Password=password"


//sensitive data should be injected from environment
System.Environment.SetEnvironmentVariable("DATABASE_CONNECTION_STRING", Constants.sqlDevConnection)

module Dtos = 
    type Inventory = JsonProvider<Constants.inventoryJson>
    type Products = JsonProvider<Constants.productsJson>

    type Article = Inventory.Inventory

    type Product = Products.Product

    type ArticleQuantity = Products.ContainArticle

module Domain =
    type Article = { 
        [<ColumnAttribute("id")>] Id : int ; 
        [<ColumnAttribute("name")>] Name: string; 
        [<ColumnAttribute("stock")>] Stock: int 
        } 
        with member this.ToDto() = 
                Dtos.Article(this.Id, this.Name, this.Stock)

    type Article
        with static member FromDto(dto : Dtos.Article) =
                { Id = dto.ArtId; Name = dto.Name; Stock = dto.Stock } 

    type ArticleQuantity = {
        [<ColumnAttribute("id")>]Id: int; 
        [<ColumnAttribute("qty")>]Qty: int 
        } 

    type Product = { 
        [<ColumnAttribute("id")>] Id : int ; 
        [<ColumnAttribute("name")>] Name: string; 
        [<ColumnAttribute("articles")>] Articles : ArticleQuantity array }
        with member this.ToDto() = 
                let articles = 
                    this.Articles 
                    |> Array.map (fun a -> Dtos.Products.ContainArticle(a.Id, a.Qty))
                Dtos.Product(this.Name, articles)


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

module Data =
    open Npgsql
    open Dapper.FSharp
    open Dapper.FSharp.PostgreSQL

    Dapper.FSharp.OptionTypes.register()

    let getConnection () =
        let connectionString = System.Environment.GetEnvironmentVariable "DATABASE_CONNECTION_STRING"
        new NpgsqlConnection(connectionString)

    let getArticles () =
        select {
            table "test.articles"
            orderBy "test.articles.name" Asc
        } 
        |> getConnection().SelectAsync<Domain.Article>
        |> Async.AwaitTask

    let getProducts () =
        select {
            table "test.products"
            innerJoin "test.products_articles" "product_id" "test.products.id"
            innerJoin "test.articles" "id" "test.products_articles.article_id"
            orderBy "Persons.Position" Asc
        } 
        |> getConnection().SelectAsync<Domain.Product>
        |> Async.AwaitTask


module Services = 
    let getArticles (httpRequest : HttpRequest) = 

        //todo: load from db and map from dto
        Data.getArticles()
        |> Async.RunSynchronously
        |> Seq.map (fun x -> x.ToDto())
        |> Seq.toArray
        |> fun x -> Dtos.Inventory.Root(x)
        |> Some
        |> Ok
        |> Utils.FromRes(Utils.FromOption(fun x -> x.JsonValue.ToString()))

    let addArticles (httpRequest : HttpRequest) = 
        let newInventoryDto =
            httpRequest.rawForm
            |> System.Text.Encoding.UTF8.GetString
            |> Dtos.Inventory.Parse
            
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
          GET  >=> request (fun r -> ServerErrors.NOT_IMPLEMENTED("ouch"))
          POST >=> request (fun r -> ServerErrors.NOT_IMPLEMENTED("ouch"))
        ]

let app =
  choose [ 
      Controllers.articlesController 
      Controllers.productsController 
      RequestErrors.NOT_FOUND "Path was not found"
  ]

startWebServer defaultConfig app