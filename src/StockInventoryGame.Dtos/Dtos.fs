namespace StockInventoryGame.Dtos

module Constants =
    [<Literal>]
    let inventoryJson = __SOURCE_DIRECTORY__ + "/json/inventory.json"

    [<Literal>]
    let productsJson = __SOURCE_DIRECTORY__ + "/json/products.json"

module ProvidedTypes =
    open FSharp.Data
    type InventoryProvider = JsonProvider<Constants.inventoryJson>
    type ProductsProvider = JsonProvider<Constants.productsJson>

module Dtos = 
    open ProvidedTypes

    type Inventory = InventoryProvider.Root
    type Article = InventoryProvider.Inventory
    type Products = ProductsProvider.Root
    type Product = ProductsProvider.Product
    type ArticleQuantity = ProductsProvider.ContainArticle
