namespace StockInventoryGame.Web

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open StockInventoryGame.Web.Configuration
open StockInventoryGame.Web
open StockInventoryGame.Web.Migrations
//open Steeltoe.Bootstrap.Autoconfig
open JsonMutator

module TerseIgnore =
    //readability trick
    let (!) a = a |> ignore

open TerseIgnore
open Microsoft.Extensions.Configuration

// ---------------------------------
// Config and Main
// ---------------------------------

module Configure =

    let configureCors (builder : CorsPolicyBuilder) =
        builder.WithOrigins("http://localhost:8080")
               .AllowAnyMethod()
               .AllowAnyHeader()
               |> ignore

    let errorHandler (ex : Exception) (logger : ILogger) =
        logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
        clearResponse >=> setStatusCode 500 >=> text ex.Message

    let webApp =
      choose [ 
          Controllers.articlesController 
          Controllers.productsController 
          RequestErrors.NOT_FOUND "Path was not found"
      ]

    let configureApp (app : IApplicationBuilder) =
        let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
        (match env.EnvironmentName with
        | "Development" -> app.UseDeveloperExceptionPage()
        | _ -> app.UseGiraffeErrorHandler(errorHandler))
            .UseHttpsRedirection()
            .UseCors(configureCors)
            .UseStaticFiles()
            .UseRouting()
            //https://github.com/microsoft/OpenAPI.NET to write the file?
            //swagger is not self generated, so we need to adjust the json manually for now..
            //.UseSwaggerUI(Action<_>(fun c -> c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1")))
            .UseGiraffe(webApp)

    let configureServices (services : IServiceCollection) (configuration :IConfiguration) =
    
        !services.AddCors()
        !services.AddGiraffe()
        !services.AddTransient<IStartupFilter, DbMigrationStartup>()
        //!services.AddHealthActuatorServices(configuration)

        //configuration using FSharp.Data type provider
        let settingsFile = "appsettings." + Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") + ".json"
        let config = AppSettingsProvider.Load(settingsFile)

        //bind/option does not work with json provided types, as they are not mutable
        let dbConnStr = Environment.GetEnvironmentVariable("DbConfiguration__ConnectionString")
        let newConfig = config.Change(<@ fun x -> x.DbConfiguration.ConnectionString = dbConnStr @>)
          
        //add once and never change (just add changes in appsettings.json!!!)
        !services.AddSingleton<AppSettings>(newConfig)
    

    let configureLogging (builder : ILoggingBuilder) =
        !builder.AddFilter(fun l -> l.Equals LogLevel.Error)
               .AddConsole()
               .AddDebug()

type Startup(configuration: IConfiguration) =

    member val Configuration = configuration with get,set
    
    member this.ConfigureServices(serviceCollection) =
        Configure.configureServices serviceCollection configuration

    member this.Configure(app) =
        Configure.configureApp(app)

module Program =
    let CreateHostBuilder(args) =
        let contentRoot = Directory.GetCurrentDirectory()
        //let webRoot = Path.Combine(contentRoot, "WebRoot")

        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(
                fun webHostBuilder ->
                    !webHostBuilder
                        .UseContentRoot(contentRoot)
                        //.UseWebRoot(webRoot)
                        .ConfigureLogging(Configure.configureLogging)
                        .UseStartup<Startup>()
                        )
            //.AddSteeltoe(loggerFactory=loggerFactory) //k8s support, actuators etc https://docs.steeltoe.io/api/v3/bootstrap/
        

    [<EntryPoint>]
    let main args =
        CreateHostBuilder(args)
            .Build()
            .Run()
        0

