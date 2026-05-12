using EaatAPI.Database;
using EaatAPI.Services;
using Global.Models;
using Global.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using Scalar.AspNetCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<EaatContext>();
builder.Services.AddHostedService<BestillingsFraBudService>();
builder.Services.AddHostedService<OutboxService>();
builder.Services.AddSingleton<ForbindTilRabbitService>();


builder.Services.AddOpenApi();

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
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
