#r "nuget:Npgsql.FSharp"
#r "nuget:FSharp.Data"
#r "nuget:Suave"

open FSharp.Data
open System
open System.Text
open System.Text.Encoding

module Constants =
    [<Literal>]
    let inventoryJson = __SOURCE_DIRECTORY__ + "/inventory.json"

    [<Literal>]
    let productsJson = __SOURCE_DIRECTORY__ + "/products.json"

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

module Result =
    type Result<'a,'b> with
        member this.IsOk = 
            match this with
            |Ok(o) -> true
            |Error(e) -> false

    type Result<'a,'b> with
        member this.getError = 
            match this with
            |Ok(o) -> None
            |Error(e) -> Some(e)

    type Result<'a,'b> with
        member this.getOk = 
            match this with
            |Ok(o) -> Some(o)
            |Error(e) -> None


module Domain =

    open Result

    type Article = { 
            Id : int ; 
            Name: string; 
            Stock: int 
        } 
        with member this.ToDto() = 
                Dtos.Article(this.Id, this.Name, this.Stock)

    type Article
        with static member FromDto(dto : Dtos.Article) =
                if dto.Stock > 0 then
                    { Id = dto.ArtId; Name = dto.Name; Stock = dto.Stock } |> Ok
                else
                    Error("stock should be > 0")

    type ArticleQuantity = {
            Id: int; 
            Qty: int 
        } 
        with member this.ToDto() = 
                Dtos.ArticleQuantity(this.Id, this.Qty)

    type ArticleQuantity 
        with static member FromDto(dto : Dtos.ArticleQuantity) =
                if dto.AmountOf > 0 then
                    { Id = dto.ArtId; Qty = dto.AmountOf } |> Ok
                else
                    Error("amountOf should be > 0")


    type Product = { 
            Id : int ; 
            Name: string; 
            Articles : ArticleQuantity list }
        with member this.ToDto() = 
                let articles = 
                    this.Articles 
                    |> Seq.map (fun a -> a.ToDto())
                    |> Array.ofSeq
                Dtos.Product(this.Name, articles)

    type Product
        with static member FromDto(dto : Dtos.Product) : Result<Product,string> =
                if dto.ContainArticles.Length > 0 then

                    let articleResults =
                        dto.ContainArticles 
                        |> Seq.map ArticleQuantity.FromDto

                    if (articleResults |> Seq.exists (fun x -> not x.IsOk )) then
                        let errorResult = articleResults |> Seq.find (fun x -> not x.IsOk)
                        Error(errorResult.getError.Value)
                    else

                        let articles = 
                            articleResults 
                            |> Seq.filter (fun x -> x.IsOk)
                            |> Seq.map (fun x -> x.getOk)
                            |> Seq.choose id
                            |> Seq.toList
                    
                        { 
                            Id = 0; 
                            Name = dto.Name; 
                            Articles = articles
                        } 
                        |> Ok
                else
                    Error("there should be at least one article")

module Data =
    open Domain
    open Npgsql.FSharp

    module Queries =

        [<Literal>]
        let getArticles =
            """SELECT x
            FROM test.articles"""

        [<Literal>]
        let getProducts =
            """SELECT
                p.id, p.prd_name, a.art_name, pa.qty
            FROM test.products as p
            INNER JOIN test.products_articles as pa
                ON pa.product_id = p.id
            INNER JOIN test.articles as a
                ON pa.article_id = a.id"""

        [<Literal>]
        let getProductByName = 
            """SELECT
                p.id, p.prd_name, a.art_name, pa.qty
            FROM test.products as p
            INNER JOIN test.products_articles as pa
                ON pa.product_id = p.id
            INNER JOIN test.articles as a
                ON pa.article_id = a.id
            WHERE p.prd_name = @prd_name"""

        [<Literal>]
        let updateArticle =
            """UPDATE a 
            SET a.stock = (a.stock - @qty)
            FROM test.articles a
            WHERE a.id = @art_id
            """

        [<Literal>]
        let setProductStatusToSold =
            """UPDATE test.products
            SET is_sold = true 
            WHERE prd_id = @prd_id
            """

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

    let getProducts () =
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
            })
        |> List.groupBy (fun p -> p.Id)
        |> List.map (fun (pid,productLines) ->
            let product = productLines |> List.head

            { 
                product with Articles = 
                    productLines 
                    |> List.collect (fun p -> p.Articles ) 
            })
       
    let getProductByName productName =
         getConnectionString()
        |> Sql.connect
        |> Sql.query Queries.getProductByName
        |> Sql.parameters [ "@prd_name", Sql.text productName ]
        |> Sql.execute (fun read ->
            let id = read.int "id"
            
            { 
                Id = id; 
                Name =  read.text "prd_name"; 
                Articles = [ {  
                        Id = read.int "id"; 
                        Qty = read.int "qty" 
                    } ]
            })
        |> List.groupBy (fun p -> p.Id)
        |> List.map (fun (pid,productLines) ->
            let product = productLines |> List.head

            { 
                product with Articles = 
                    productLines 
                    |> List.collect (fun p -> p.Articles ) 
            })
        |> fun l ->
            match l with
            |[] -> None
            |x::[] -> Some(x)
            |_ -> failwith "db shouldnt allow duplicate product names"

    /// transactional update of inventory and product
    let trySellProduct (product: Product) =

        // This query is executed n times in a single sql script
        let updateArticlesT =
            let executions =
                product.Articles 
                |> List.map (fun a -> 
                    [ 
                        "@qty", Sql.int a.Qty 
                        "@art_id", Sql.int64 (a.Id |> int64) 
                    ]
                ) 
            Queries.updateArticle, executions

        let updateProductStatusT =
            Queries.setProductStatusToSold, [ 
                [ "@prd_id", Sql.int64 (product.Id |> int64) ] 
            ]

        try
            getConnectionString()
            |> Sql.connect
            |> Sql.executeTransaction 
                [
                    updateArticlesT
                    updateProductStatusT
                ]
            |> fun x -> x.Length //affected rows
            |> Ok

        with ex ->
            Error(ex.Message)

    /// transactional update of inventory and product
    let tryAddArticles (articles: Article list) =

        // This query is executed n times in a single sql script
        let updateArticlesT =
            let executions =
                product.Articles 
                |> List.map (fun a -> 
                    [ 
                        "@qty", Sql.int a.Qty 
                        "@art_id", Sql.int64 (a.Id |> int64) 
                    ]
                ) 
            Queries.updateArticle, executions

        let updateProductStatusT =
            Queries.setProductStatusToSold, [ 
                [ "@prd_id", Sql.int64 (product.Id |> int64) ] 
            ]

        try
            getConnectionString()
            |> Sql.connect
            |> Sql.executeTransaction 
                [
                    updateArticlesT
                    updateProductStatusT
                ]
            |> fun x -> x.Length //affected rows
            |> Ok

        with ex ->
            Error(ex.Message)



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

    let FromWebPartOption webPartOption  = 
        match webPartOption with
        |Some(wp) -> 
            wp
        |None -> 
            RequestErrors.NOT_FOUND("not found")


module Services = 

    open Result

    let getArticles (httpRequest : HttpRequest) = 
        Data.getArticles()
        //|> Async.RunSynchronously
        |> Seq.map (fun x -> x.ToDto())
        |> Seq.toArray
        |> fun x -> Dtos.Inventory(x)
        |> Some
        |> Ok
        |> Utils.FromRes(Utils.FromOption(fun x -> x.JsonValue.ToString()))

    let addArticles (httpRequest : HttpRequest) = 
        let addArticlesJson =
            httpRequest.rawForm
            |> System.Text.Encoding.UTF8.GetString

        let newInventoryDto =
            addArticlesJson
            |> JsonValue.Parse
            |> Dtos.Inventory
            
        let domainArticles = 
            newInventoryDto
            |> fun x -> x.Inventory
            |> Array.map (fun x -> Domain.Article.FromDto)

        Data.addArticles(domainArticles)
        
        //POST should replace all (idempotent?)
        newInventoryDto
        |> fun x -> x.JsonValue.ToString()
        |> Ok

    let getProducts (httpRequest : HttpRequest) = 
        Data.getProducts()
        //|> Async.RunSynchronously
        |> Seq.map (fun x -> x.ToDto())
        |> Seq.toArray
        |> fun x -> Dtos.Products(x)
        |> Some
        |> Ok
        |> Utils.FromRes(Utils.FromOption(fun x -> x.JsonValue.ToString()))

    let addProducts (httpRequest : HttpRequest) = 
        
        let addProductsJson =
            httpRequest.rawForm
            |> System.Text.Encoding.UTF8.GetString
        
        let newProductsDto =
            addProductsJson
            |> JsonValue.Parse
            |> Dtos.Products
            
        let productResult = 
            newProductsDto
            |> fun x -> x.Products
            |> Array.map Domain.Product.FromDto

        let result = 
            if productResult |> Seq.exists (fun x -> not x.IsOk) then
                Error($"some products had errors, {1}")
            else
                //POST should replace all (idempotent?)
                Ok(newProductsDto :> Dtos.Products)

        result 
        |> Utils.FromRes(fun x -> OK(addProductsJson))
               

    let sellProduct (httpRequest : HttpRequest) = 
        
        let productJsonString =
            System.Text.Encoding.UTF8.GetString(httpRequest.rawForm)

        let productDto : Dtos.Product =
            productJsonString
            |> JsonValue.Parse
            |> Dtos.Product

        Data.getProductByName(productDto.Name)
        |> Option.map Data.trySellProduct
        |> Option.map (fun r ->
            if (r.IsOk && r.getOk.Value > 0) then
                OK(productJsonString)
            else
                NO_CONTENT
        )
        |> Utils.FromWebPartOption

    

module Controllers = 
    let articlesController : WebPart = 
        path "/articles" >=> choose [
          GET  >=> request Services.getArticles
          POST >=> request Services.addArticles
        ]

    let productsController : WebPart = 
        path "/products" >=> choose [
          GET  >=> request  Services.getProducts
          POST >=> request  Services.addProducts
          path "/sell" >=> choose [ 
              POST >=> request  Services.sellProduct 
          ]
        ]

let app =
  choose [ 
      Controllers.articlesController 
      Controllers.productsController 
      RequestErrors.NOT_FOUND "Path was not found"
  ]

startWebServer defaultConfig app