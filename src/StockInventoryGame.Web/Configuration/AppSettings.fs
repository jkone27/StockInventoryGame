namespace StockInventoryGame.Web.Configuration

open FSharp.Data
open Microsoft.Extensions.Configuration

type AppSettingsProvider = JsonProvider<"appsettings.json">
type AppSettings = AppSettingsProvider.Root

type AppSettingsLoader(configuration: IConfiguration) =
    member val Current = 
        configuration.GetValue<string>("") 
        |> AppSettingsProvider.Parse