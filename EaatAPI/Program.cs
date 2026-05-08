using EaatAPI.Database;
using EaatAPI.Models;
using EaatAPI.Services;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<EaatContext>();
builder.Services.AddHostedService<BestillingsFraBudService>();
var factory = new ConnectionFactory() { HostName = "localhost" };
var connection = await factory.CreateConnectionAsync();
var channel = await connection.CreateChannelAsync();
await channel.ExchangeDeclareAsync(exchange: "bestillingerFraAPITilRestaurant", type: ExchangeType.Direct, durable: true);
await channel.ExchangeDeclareAsync(exchange: "bestillingerFraBud", type: ExchangeType.Direct, durable: true);
builder.Services.AddSingleton<IConnection>(connection);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EaatContext>();
    context.Database.EnsureCreated();

    if (!context.Kunder.Any())
    {
        var kunde1 = new Kunde("Anders", "Klosterport 2A");
        var kunde2 = new Kunde("Mette", "Skolegade 3");
        var restaurant1 = new Restaurant("PizzariaManden", "Tøndervej 32");
        var restaurant2 = new Restaurant("SushiManden", "Sushivej 27");

        context.Kunder.AddRange(kunde1, kunde2);
        context.Restauranter.AddRange(restaurant1, restaurant2);

        context.SaveChanges();

        var bestilling1 = new Bestilling("Stor pizza", restaurant1.Adresse, kunde1.Adresse, kunde1.Id, restaurant1.Id);
        var bestilling2 = new Bestilling("Lille sushibakke", restaurant2.Adresse, kunde2.Adresse, kunde2.Id, restaurant2.Id);
        var bud1 = new Bud();
        var bud2 = new Bud();
        context.Bestillinger.AddRange(bestilling1, bestilling2);
        context.Buds.AddRange(bud1, bud2);

        context.SaveChanges();


    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
