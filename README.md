# Stock Inventory Game

[Install dotnet-sdk](https://dotnet.microsoft.com/download)

## Script

you can test the first script POC solution via dotnet fsharp interactive

```cli
dotnet fsi .\script\Poc.fsx
```

and you can test via [vscode rest script](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) a nice http dsl
from .\script\test.http

if you are on windows you can use visual studio else you can install vscode and [ionide ext](https://ionide.io/)

## Database

Set up local test db via docker run

```cli
docker run --name localdb -e POSTGRES_PASSWORD=password -d -p 5432:5432 postgres
```

The storage uses PGSQL, so you can use any pgsql client to connect to it,  
DB init script are in provided in  
`` .\script\dbInit.pgsql ``  
and some manual test scripts in  
`` .\script\dbTests.pgsql ``  

## Compiled Version

to run the compile solution go to src folder and run

```cli
dotnet run 
```

extra, install [pgsql analyzer](https://github.com/Zaid-Ajaj/Npgsql.FSharp.Analyzer) to cath sql query errors from your ide, a vs extension is also available

```cli
dotnet tool install --global Paket
paket init
paket add NpgsqlFSharpAnalyzer --group Analyzers
paket install
```

integration tests where not completed so they are currently failing, 
normally you would run them with the commands (TBD), they use Xunit framework

```cli
cd test
dotnet test
```
