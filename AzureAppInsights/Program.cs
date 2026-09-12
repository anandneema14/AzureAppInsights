using AzureAppInsights.Models;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration.GetSection("Mongo")["ConnectionString"]));
builder.Services.AddSingleton(sp =>
{
    var mongo = builder.Configuration.GetSection("Mongo");
    return sp.GetRequiredService<IMongoClient>().GetDatabase(mongo["Database"]);
});
builder.Services.AddSingleton(sp =>
{
    var mongo = builder.Configuration.GetSection("Mongo");
    return sp.GetRequiredService<IMongoDatabase>()
        .GetCollection<Product>(mongo["ProductsCollection"]);
});

var app = builder.Build();

// Seed a few products on first run.
await using (var scope = app.Services.CreateAsyncScope())
{
    var products = scope.ServiceProvider.GetRequiredService<IMongoCollection<Product>>();
    if (await products.CountDocumentsAsync(FilterDefinition<Product>.Empty) == 0)
    {
        await products.InsertManyAsync(new[]
        {
            new Product { Name = "Keyboard", Price = 79.99m },
            new Product { Name = "Mouse", Price = 29.99m },
            new Product { Name = "Monitor", Price = 349.00m }
        });
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "AzureAppInsights v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
