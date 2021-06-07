namespace  StockInventoryGame.Services

type ErrorResult = 
    | UnrecoverableError of string 
    | ValidationError of string
    | NotFound

type Success = Completed | NoOperation | Response of string

module Services =

    open StockInventoryGame.Domain.Result
    open StockInventoryGame.Domain
    open StockInventoryGame.Data
    open StockInventoryGame.Dtos

    let getArticles () = 
        Data.getArticles()
        //|> Async.RunSynchronously
        |> Seq.map (fun x -> x.ToDto())
        |> Seq.toArray
        |> fun x -> Dtos.Inventory(x)

    let addArticles (newInventoryDto : Dtos.Inventory) = 
        let domainArticlesResults = 
            newInventoryDto
            |> fun x -> x.Inventory
            |> Array.map Domain.Article.FromDto

        if (domainArticlesResults |> Seq.exists (fun x -> not x.IsOk)) then
            let firstError = 
                domainArticlesResults
                |> Seq.find (fun x -> not x.IsOk)

            Error(ValidationError(firstError.getError.Value))

        else
            
            let articleList =
                domainArticlesResults
                |> Seq.filter (fun x -> x.IsOk)
                |> Seq.map (fun x -> x.getOk)
                |> Seq.choose id
                |> Seq.toList

            match Data.tryAddArticles(articleList) with
            |Ok(rowCount) ->
                if rowCount > 0 then
                    Ok(Completed)
                else
                    Ok(NoOperation)
            |Error(err) ->
                Error(UnrecoverableError(err))
        
   

    let getProducts () = 
        Data.getProducts()
        //|> Async.RunSynchronously
        |> Seq.map (fun x -> x.ToDto())
        |> Seq.toArray
        |> fun x -> Dtos.Products(x)

    let addProducts (newProductsDto : Dtos.Products) = 
        
        let productsResult = 
            newProductsDto
            |> fun x -> x.Products
            |> Array.map Domain.Product.FromDto

        if (productsResult |> Seq.exists (fun x -> not x.IsOk)) then
            let firstError = 
                productsResult
                |> Seq.find (fun x -> not x.IsOk)
            
            Error(ValidationError(firstError.getError.Value))

        else
            
            let productsList =
                productsResult
                |> Seq.filter (fun x -> x.IsOk)
                |> Seq.map (fun x -> x.getOk)
                |> Seq.choose id
                |> Seq.toList

            // let requiredArticles =
            //     productsList
            //     |> List.collect (fun p -> p.Articles)
            //     |> List.map (fun a -> a.Id)
            //     |> Seq.ofList //merge dups

            match Data.tryAddProducts(productsList) with
            |Ok(rowCount) ->
                if rowCount > 0 then
                    Ok(Completed)
                else
                    Ok(NoOperation)
            |Error(err) ->
                Error(UnrecoverableError(err))
        
               

    let sellProduct (productDto : Dtos.Product) = 

        let opt =
            Data.getProductByName(productDto.Name)
            |> Option.map Data.trySellProduct
            |> Option.map (fun r ->
                if (r.IsOk && r.getOk.Value > 0) then
                    Ok(Completed)
                else
                    Ok(NoOperation)
            )
        match opt with
        |Some(x) -> x
        |None -> Error(NotFound)

    