using EaatAPI.Database;
using Global.Models;
using Global.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace EaatAPI.Services
{
    public class BestillingsFraBudService : IHostedService
    {
        private ForbindTilRabbitService _forbindTilRabbitService;
        private readonly IServiceProvider _serviceProvider;
        private IChannel _channel;


        public BestillingsFraBudService(ForbindTilRabbitService forbindTilRabbitService, IServiceProvider serviceProvider)
        {
            _forbindTilRabbitService = forbindTilRabbitService;
            _serviceProvider = serviceProvider;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var connection = await _forbindTilRabbitService.GetConnectionAsync();
            _channel = await connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(exchange: "bestillingerFraBud", type: ExchangeType.Direct, durable: true);
            QueueDeclareOk queueDeclareResult = await _channel.QueueDeclareAsync();
            string queueName = queueDeclareResult.QueueName;
            await _channel.QueueBindAsync(queue: queueName, exchange: "bestillingerFraBud", routingKey: string.Empty);

            Console.WriteLine("Venter på bud...");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                var bestillingsOpdatering = JsonSerializer.Deserialize<Bestilling>(message);

                if (bestillingsOpdatering != null)
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<EaatContext>();
                        Bestilling bestilling = await context.Bestillinger.FindAsync(bestillingsOpdatering.Id); 
                        if (bestilling != null)
                        {
                            bestilling.BudId = bestillingsOpdatering.BudId;
                            context.Bestillinger.Update(bestilling);
                            await context.SaveChangesAsync();

                            var updateMessage = JsonSerializer.Serialize(bestilling);
                            var body = Encoding.UTF8.GetBytes(updateMessage);

                            await _channel.BasicPublishAsync(exchange: "bestillingerTilBud", routingKey: string.Empty, body: body);
                        }
                    }
                }
            };

            await _channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);
            Console.WriteLine("BestillingFraBudservice er startet og lytter...");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Bud-service stoppet.");
            return Task.CompletedTask;
        }
    }
}
