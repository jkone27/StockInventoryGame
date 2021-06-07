namespace StockInventoryGame.Domain

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
    open StockInventoryGame.Dtos

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

