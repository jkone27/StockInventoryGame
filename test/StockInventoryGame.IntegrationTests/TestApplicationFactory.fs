namespace StockInventoryGame.IntegrationTests

open Microsoft.AspNetCore.Mvc.Testing
open StockInventoryGame.Web
open Microsoft.Extensions.Configuration
open StockInventoryGame.Web.TerseIgnore
open StockInventoryGame.Web.Migrations
open StockInventoryGame.Web
open System.IO
open Microsoft.Extensions.DependencyInjection.Extensions
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.AspNetCore.Hosting

module Constants =
    [<Literal>]
    let sqlDevConnection = "Host=localhost;Database=postgres;Username=postgres;Password=password"

type TestApplicationFactory() =

    inherit WebApplicationFactory<Startup>()

    do
        System.Environment.SetEnvironmentVariable("DbConfiguration__ConnectionString", Constants.sqlDevConnection)
        System.Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT","IntegrationTests")
     
    override this.ConfigureWebHost(builder) =

        !builder
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseEnvironment("IntegrationTests")
            .ConfigureAppConfiguration(fun cfg ->
                !cfg.AddJsonFile("appsettings.IntegrationTests.json")
                    .AddEnvironmentVariables()
            )
            .ConfigureServices(fun builder services ->
            
                let startupFilter = 
                    services 
                    |> Seq.find (fun d -> d.ImplementationType = typeof<DbMigrationStartup>)
            
                //load migrations if any
                !services.Remove(startupFilter)
            )
            .Configure(fun app ->
                let migrationStartupFilter = 
                    app.ApplicationServices.GetRequiredService<IStartupFilter>()
                    
                !migrationStartupFilter.Configure(fun _ -> ())
            )