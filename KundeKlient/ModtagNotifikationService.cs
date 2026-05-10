using Global.Models;
using Global.Services;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace KundeKlient
{
    internal class ModtagNotifikationService : IHostedService
    {
        private ForbindTilRabbitService _forbindTilRabbitService;
        private IChannel _channel;
        public static Action<Bestilling>? OnNotificationReceived;
        public static int KundeId { get; set; }
        public ModtagNotifikationService(ForbindTilRabbitService forbindTilRabbitService)
        {
            _forbindTilRabbitService = forbindTilRabbitService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            bool forbundet = false;
            string queueName = string.Empty;

            while (!forbundet && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var connection = await _forbindTilRabbitService.GetConnectionAsync();
                    _channel = await connection.CreateChannelAsync();

                    await _channel.ExchangeDeclareAsync(exchange: "notifikationTilKunde", type: ExchangeType.Direct, durable: true);

                    queueName = $"kundeQueue_{KundeId}";
                    QueueDeclareOk queueDeclareResult = await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);

                    await _channel.QueueBindAsync(queue: queueName, exchange: "notifikationTilKunde", routingKey: KundeId.ToString());

                    forbundet = true;
                    Console.WriteLine("Forbundet til RabbitMQ!");
                }
                catch (Exception)
                {
                    Console.WriteLine("Kunne ikke forbinde til RabbitMQ. Prøver igen om 3 sekunder...");
                    await Task.Delay(3000, cancellationToken);
                }
            }

            if (_channel == null) return;

            Console.WriteLine("Venter på at ordrer bliver accepteret...");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += (model, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                var bestilling = JsonSerializer.Deserialize<Bestilling>(message);

                if (bestilling != null)
                {
                    OnNotificationReceived.Invoke(bestilling);
                }
                return Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);
            Console.WriteLine("Bud-service er startet og lytter...");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
