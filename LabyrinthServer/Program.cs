using LabyrinthServer;
using Microsoft.AspNetCore.Mvc;
using Dto = ApiTypes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Labyrinth Training Server API",
        Version = "v1",
        Description = "API for training labyrinth crawlers"
    });
});

// Singleton pour gérer l'état du jeu
builder.Services.AddSingleton<LabyrinthGame>();

var app = builder.Build();

// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ============================================================================
// ENDPOINTS
// ============================================================================

/// <summary>
/// POST /crawlers - Créer un nouveau crawler
/// </summary>
app.MapPost("/crawlers", (
    [FromQuery] Guid appKey,
    [FromBody] Dto.Settings? settings,
    [FromServices] LabyrinthGame game) =>
{
    var crawler = game.CreateCrawler(appKey, settings);
    return Results.Ok(crawler);
})
.WithName("CreateCrawler")
.WithOpenApi(operation =>
{
    operation.Summary = "Create a new crawler";
    operation.Description = "Creates a new crawler at the starting position with the specified settings";
    return operation;
})
.Produces<Dto.Crawler>(StatusCodes.Status200OK);

/// <summary>
/// PATCH /crawlers/{crawlerId} - Mettre à jour un crawler (changer direction ou marcher)
/// </summary>
app.MapPatch("/crawlers/{crawlerId}", (
    [FromRoute] Guid crawlerId,
    [FromQuery] Guid appKey,
    [FromBody] Dto.Crawler updates,
    [FromServices] LabyrinthGame game) =>
{
    var result = game.UpdateCrawler(crawlerId, appKey, updates);
    return result is not null
        ? Results.Ok(result)
        : Results.NotFound(new { error = "Crawler not found or invalid app key" });
})
.WithName("UpdateCrawler")
.WithOpenApi(operation =>
{
    operation.Summary = "Update crawler state";
    operation.Description = "Updates the crawler's direction and/or attempts to walk forward";
    return operation;
})
.Produces<Dto.Crawler>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

/// <summary>
/// DELETE /crawlers/{crawlerId} - Supprimer un crawler
/// </summary>
app.MapDelete("/crawlers/{crawlerId}", (
    [FromRoute] Guid crawlerId,
    [FromQuery] Guid appKey,
    [FromServices] LabyrinthGame game) =>
{
    var success = game.DeleteCrawler(crawlerId, appKey);
    return success
        ? Results.NoContent()
        : Results.NotFound(new { error = "Crawler not found or invalid app key" });
})
.WithName("DeleteCrawler")
.WithOpenApi(operation =>
{
    operation.Summary = "Delete a crawler";
    operation.Description = "Removes a crawler from the game";
    return operation;
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

/// <summary>
/// PUT /crawlers/{crawlerId}/{inventoryType} - Déplacer des items entre inventaires
/// </summary>
app.MapPut("/crawlers/{crawlerId}/{inventoryType}", (
    [FromRoute] Guid crawlerId,
    [FromRoute] string inventoryType,
    [FromQuery] Guid appKey,
    [FromBody] Dto.InventoryItem[] items,
    [FromServices] LabyrinthGame game) =>
{
    var result = game.MoveItems(crawlerId, appKey, inventoryType, items);
    return result is not null
        ? Results.Ok(result)
        : Results.NotFound(new { error = "Crawler not found or invalid operation" });
})
.WithName("MoveItems")
.WithOpenApi(operation =>
{
    operation.Summary = "Move items between inventories";
    operation.Description = "Moves items from one inventory to another (bag <-> items)";
    return operation;
})
.Produces<Dto.InventoryItem[]>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// Endpoint racine pour vérifier que le serveur fonctionne
app.MapGet("/", () => new
{
    service = "Labyrinth Training Server",
    version = "1.0",
    status = "running",
    documentation = "/swagger"
})
.WithName("Root")
.ExcludeFromDescription();

app.Run();
