using EaatAPI.Database;
using EaatAPI.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EaatAPI.Services
{
    public class BestillingsFraBudService : IHostedService
    {
        private IConnection? _connection;
        private readonly IServiceProvider _serviceProvider;
        private IChannel _channel;


        public BestillingsFraBudService(IConnection connection, IServiceProvider serviceProvider)
        {
            _connection = connection;
            _serviceProvider = serviceProvider;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            _channel = await _connection.CreateChannelAsync();

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

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_connection is not null) await _connection.CloseAsync();

            Console.WriteLine("Bud-service stoppet.");
        }
    }
}
