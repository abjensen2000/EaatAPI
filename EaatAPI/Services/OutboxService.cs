using EaatAPI.Database;
using Global.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EaatAPI.Services
{
    public class OutboxService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ForbindTilRabbitService _forbindTilRabbit;

        public OutboxService(IServiceProvider serviceProvider, ForbindTilRabbitService forbindTilRabbit)
        {
            _serviceProvider = serviceProvider;
            _forbindTilRabbit = forbindTilRabbit;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connection = await _forbindTilRabbit.GetConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(exchange: "bestillingerFraAPITilRestaurant", type: ExchangeType.Direct, durable: true);
            await channel.ExchangeDeclareAsync(exchange: "bestillingerFraRestaurantTilBud", type: ExchangeType.Fanout, durable: true);
            await channel.ExchangeDeclareAsync(exchange: "notifikationTilKunde", type: ExchangeType.Direct, durable: true);

            Console.WriteLine("Outbox-service er startet og overvåger for usendte beskeder...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<EaatContext>();
                        var usendte = context.OutboxMessages.Where(m => !m.ErSendt).ToList();

                        if (usendte.Any())
                        {
                            foreach (var besked in usendte)
                            {
                                var body = Encoding.UTF8.GetBytes(besked.Payload);
                                await channel.BasicPublishAsync(exchange: besked.ExchangeName, routingKey: besked.RoutingKey, body: body, cancellationToken: stoppingToken);
                                besked.ErSendt = true;
                            }
                            await context.SaveChangesAsync(stoppingToken); //Annullerer transaktionen hvis processen stopper
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Outbox-Service fejl i overvågning: {ex.Message}");
                }
                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}
