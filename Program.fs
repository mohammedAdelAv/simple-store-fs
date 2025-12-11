open System
open System.IO
open System.Text.Json
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.FileProviders
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open simpleStore.Services
open simpleStore.Domain

[<EntryPoint>]
let main argv =
    let builder = WebApplication.CreateBuilder()
    builder.Services.AddCors(fun options ->
        options.AddDefaultPolicy(fun policy ->
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod() |> ignore
        ) |> ignore
    ) |> ignore

    let app = builder.Build()

    // serve static files from WebUI folder
    let webUiPath = Path.Combine(Directory.GetCurrentDirectory(), "WebUI")
    if Directory.Exists(webUiPath) then
        // use 'new' for IDisposable types
        let fileProvider = new PhysicalFileProvider(webUiPath)
        let defaultFilesOptions = new DefaultFilesOptions(FileProvider = fileProvider)
        app.UseDefaultFiles(defaultFilesOptions) |> ignore
        let staticOptions = new StaticFileOptions(FileProvider = fileProvider)
        app.UseStaticFiles(staticOptions) |> ignore
    else
        printfn "Warning: WebUI folder not found at %s" webUiPath

    app.UseCors() |> ignore

    // GET /api/products
    app.MapGet("/api/products", Func<HttpContext, Task<IResult>>(fun (ctx: HttpContext) ->
        task {
            try
                let webJson = Path.Combine(webUiPath, "products.json")
                if File.Exists(webJson) then
                    let json = File.ReadAllText(webJson)
                    return Results.Text(json, "application/json")
                else
                    let catalog = Catalog.initCatalog() |> Map.toList |> List.map snd
                    let opts = JsonSerializerOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)
                    let json = JsonSerializer.Serialize(catalog, opts)
                    return Results.Text(json, "application/json")
            with ex ->
                return Results.Problem(ex.Message)
        }
    )) |> ignore

    // POST /api/cart
    app.MapPost("/api/cart", Func<HttpContext, Task<IResult>>(fun (ctx: HttpContext) ->
        task {
            try
                use sr = new StreamReader(ctx.Request.Body)
                let! body = sr.ReadToEndAsync()
                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "cart.json"), body)
                return Results.Ok(box "cart saved")
            with ex ->
                return Results.Problem(ex.Message)
        }
    )) |> ignore

    // POST /api/receipt
    app.MapPost("/api/receipt", Func<HttpContext, Task<IResult>>(fun (ctx: HttpContext) ->
        task {
            try
                use sr = new StreamReader(ctx.Request.Body)
                let! body = sr.ReadToEndAsync()
                let receiptsPath = Path.Combine(Directory.GetCurrentDirectory(), "receipts.json")
                if not (File.Exists(receiptsPath)) then
                    File.WriteAllText(receiptsPath, "[\n" + body + "\n]")
                else
                    let existing = File.ReadAllText(receiptsPath).Trim()
                    if existing.EndsWith("]") then
                        let prefix = existing.Substring(0, existing.Length - 1).TrimEnd()
                        let newJson =
                            if prefix.EndsWith("[") then
                                prefix + "\n" + body + "\n]"
                            else
                                prefix + ",\n" + body + "\n]"
                        File.WriteAllText(receiptsPath, newJson)
                    else
                        File.WriteAllText(receiptsPath, "[\n" + body + "\n]")
                return Results.Ok(box "receipt saved")
            with ex ->
                return Results.Problem(ex.Message)
        }
    )) |> ignore

    app.Run()
    0
