using Global.Models;
using Global.Services;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace BudKlient
{
    internal class ModtagBestillingService : IHostedService
    {
        private ForbindTilRabbitService _forbindTilRabbitService;
        private IChannel? _channel;
        private static readonly ConcurrentDictionary<int, Bestilling> _bestillinger = new();
        public static Action? OnMessageReceived;
        public static ConcurrentDictionary<int, Bestilling> Bestillinger => _bestillinger;

        public ModtagBestillingService(ForbindTilRabbitService forbindTilRabbitService)
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

                    await _channel.ExchangeDeclareAsync(exchange: "bestillingerFraRestaurantTilBud", type: ExchangeType.Fanout, durable: true);

                    QueueDeclareOk queueDeclareResult = await _channel.QueueDeclareAsync();
                    queueName = queueDeclareResult.QueueName;

                    await _channel.QueueBindAsync(queue: queueName, exchange: "bestillingerFraRestaurantTilBud", routingKey: string.Empty);

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

            Console.WriteLine("Venter på ordrer...");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += (model, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                var bestilling = JsonSerializer.Deserialize<Bestilling>(message);

                if (bestilling != null)
                {
                    if (bestilling.BudId == 0 || bestilling.BudId == null)
                    {
                        if (!_bestillinger.ContainsKey(bestilling.Id))
                        {
                            _bestillinger.TryAdd(bestilling.Id, bestilling);
                        }
                    }
                    else
                    {
                        _bestillinger.TryRemove(bestilling.Id, out _);
                    }
                    OnMessageReceived?.Invoke();
                }
                return Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);
            Console.WriteLine("Bud-service er startet og lytter...");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Bud-service stoppet.");
            return Task.CompletedTask;
        }
    }
}

