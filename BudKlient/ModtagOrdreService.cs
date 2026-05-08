using EaatAPI.Models;
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
    internal class ModtagOrdreService : IHostedService
    {
        private IConnection? _connection;
        private IChannel? _channel;
        private static readonly ConcurrentBag<Bestilling> _bestillinger = new();
        public static Action? OnMessageReceived;

        public static ConcurrentBag<Bestilling> Bestillinger => _bestillinger;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            bool forbundet = false;
            string queueName = string.Empty;

            while (!forbundet && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var factory = new ConnectionFactory() { HostName = "localhost" };
                    _connection = await factory.CreateConnectionAsync();
                    _channel = await _connection.CreateChannelAsync();

                    await _channel.ExchangeDeclareAsync(exchange: "bestillingerTilBud", type: ExchangeType.Fanout, durable: true);

                    QueueDeclareOk queueDeclareResult = await _channel.QueueDeclareAsync();
                    queueName = queueDeclareResult.QueueName;

                    await _channel.QueueBindAsync(queue: queueName, exchange: "bestillingerTilBud", routingKey: string.Empty);

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
                        if (!_bestillinger.Any(b => b.Id == bestilling.Id))
                        {
                            _bestillinger.Add(bestilling);
                        }
                    }
                    else
                    {
                        var eksisterende = _bestillinger.FirstOrDefault(i => i.Id == bestilling.Id);
                        if (eksisterende != null)
                        {
                            var midlertidigListe = _bestillinger.Where(b => b.Id != bestilling.Id).ToList();
                            _bestillinger.Clear();
                            foreach (var b in midlertidigListe)
                            {
                                _bestillinger.Add(b);
                            }
                        }
                    }
                    OnMessageReceived?.Invoke();
                }
                return Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);
            Console.WriteLine("Bud-service er startet og lytter...");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel is not null) await _channel.CloseAsync();
            if (_connection is not null) await _connection.CloseAsync();

            Console.WriteLine("Bud-service stoppet.");
        }


    }
}

