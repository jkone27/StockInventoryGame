namespace StockInventoryGame.Data

    module Constants =
        [<Literal>]
        let sqlDevConnection = "Host=localhost;Database=postgres;Username=postgres;Password=password"

    module Data =
        open StockInventoryGame.Domain.Domain
        open Npgsql
        open Npgsql.FSharp
        open System.Transactions

        [<Literal>]
        let getArticlesQuery = """SELECT id, art_name, stock FROM test.articles"""

        [<Literal>]
        let getProductsQuery =
            """SELECT
                p.id as prd_id, a.id as art_id, p.prd_name, a.art_name, pa.qty
            FROM test.products as p
            INNER JOIN test.products_articles as pa
                ON pa.product_id = p.id
            INNER JOIN test.articles as a
                ON pa.article_id = a.id"""

        [<Literal>]
        let getProductByNameQuery = 
            """SELECT
                p.id as prd_id, a.id as art_id, p.prd_name, a.art_name, pa.qty
            FROM test.products as p
            INNER JOIN test.products_articles as pa
                ON pa.product_id = p.id
            INNER JOIN test.articles as a
                ON pa.article_id = a.id
            WHERE p.prd_name = @prd_name"""

        [<Literal>]
        let updateArticleQuery =
            """UPDATE a 
            SET a.stock = (a.stock - @qty)
            FROM test.articles a
            WHERE a.id = @art_id
            """

        [<Literal>]
        let setProductStatusToSoldQuery =
            """UPDATE test.products
            SET is_sold = true 
            WHERE prd_id = @prd_id
            """

        [<Literal>]
        let insertArticleQuery =
            "INSERT INTO test.articles(id, art_name, stock) VALUES(@id,@art_name,@stock) RETURNING *"

        [<Literal>]
        let insertProductQuery =
            "INSERT INTO test.products (prd_name, is_sold)
             VALUES (@prd_name, false) RETURNING id as newprd_id"

        //they go together, the second line is repeated multiple times
        [<Literal>]
        let insertProductLineQuery =
            "INSERT INTO test.products_articles(product_id, article_id, qty) 
             VALUES (@prd_id, @art_id, @qty)"

        let getConnectionString () =
            "Host=localhost;Database=postgres;Username=postgres;Password=password"
            //System.Environment.GetEnvironmentVariable "DATABASE_CONNECTION_STRING"

        let getArticles () =
            getConnectionString()
            |> Sql.connect
            |> Sql.query getArticlesQuery
            |> Sql.execute (fun read ->
                {
                    Id = read.int "id"
                    Name = read.text "art_name"
                    Stock = read.int "stock"
                })

        let getProducts () =
            getConnectionString()
            |> Sql.connect
            |> Sql.query getProductsQuery
            |> Sql.execute (fun read ->
                let id = read.int "prd_id"
            
                { 
                    Id = id; 
                    Name =  read.text "prd_name"; 
                    Articles = [ {  
                            Id = read.int "art_id"; 
                            Qty = read.int "qty" 
                        } ]
                } )
            |> List.groupBy (fun p -> p.Id)
            |> List.map (fun (pid,productLines) ->
                let product = productLines |> List.head

                { 
                    product 
                    with Articles = 
                            productLines 
                            |> List.collect (fun p -> p.Articles ) 
                } )
       
        let getProductByName productName =
             getConnectionString()
            |> Sql.connect
            |> Sql.query getProductByNameQuery
            |> Sql.parameters [ "@prd_name", Sql.text productName ]
            |> Sql.execute (fun read ->
                let id = read.int "prd_id"
                { 
                    Id = id; 
                    Name =  read.text "prd_name"; 
                    Articles = [ {  
                            Id = read.int "art_id"; 
                            Qty = read.int "qty" 
                        } ]
                })
            |> List.groupBy (fun p -> p.Id)
            |> List.map (fun (_,productLines) ->
                let product = productLines |> List.head
                { 
                    product 
                    with Articles = 
                            productLines 
                            |> List.collect (fun p -> p.Articles ) 
                })
            |> fun l ->
                match l with
                |[] -> None
                |x::[] -> Some(x)
                |_ -> failwith "db shouldnt allow duplicate product names"

        /// transactional update of inventory and product
        let trySellProduct product =

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
                updateArticleQuery, executions

            let updateProductStatusT =
                setProductStatusToSoldQuery, [ 
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

        /// transactional
        let tryAddArticles (articles: Article list) =

            // This query is executed n times in a single sql script
            let addArticlesT =
                let executions =
                    articles
                    |> List.map (fun a -> 
                        [ 
                            "@id", Sql.int64 (a.Id |> int64) 
                            "@art_name", Sql.text a.Name
                            "@stock", Sql.int64 (a.Stock |> int64)
                        ]
                    ) 
                insertArticleQuery, executions

            try
                getConnectionString()
                |> Sql.connect
                |> Sql.executeTransaction 
                    [
                        addArticlesT
                    ]
                |> fun x -> x.Length //affected rows
                |> Ok

            with ex ->
                Error(ex.Message)

        /// transactional
        let tryAddProducts (products: Product list) =

            try
                use scope = new TransactionScope()
                use connection = new NpgsqlConnection(getConnectionString())
                connection.Open()

                let affected = new ResizeArray<int64>()
                for product in products do
                    let newProductId = 
                        connection
                        |> Sql.existingConnection
                        |> Sql.query insertProductQuery
                        |> Sql.parameters [ "@prd_name", Sql.text product.Name ]
                        |> Sql.execute(fun read -> read.int64 "newprd_id")
                        |> Seq.head


                    let productLines = products |> List.collect (fun p -> p.Articles)

                    for line in productLines do
                        connection
                        |> Sql.existingConnection
                        |> Sql.query insertProductLineQuery
                        |> Sql.parameters 
                            [ 
                                ("@prd_id", Sql.int64 newProductId)
                                ("@art_id", Sql.int64 (line.Id |> int64))
                                ("@qty", Sql.int line.Qty)
                            ]
                        |> Sql.executeNonQuery
                        |> ignore

                    affected.Add(newProductId) |> ignore

                if affected.Count <> products.Length then
                    failwith($"update partially failed, rolling back")
                else
                    scope.Complete()
                    Ok(products.Length)
            with ex ->
                Error(ex.Message)

