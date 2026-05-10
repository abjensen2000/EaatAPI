using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using Polly;
using Polly.Retry;

namespace Global.Services
{
    public class ForbindTilRabbitService
    {
        private ConnectionFactory _factory;
        private ResiliencePipeline _pipeline;
        private IConnection? _connection;


        public ForbindTilRabbitService()
        {
            _factory = new ConnectionFactory() {HostName = "localhost" };

            _pipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    MaxRetryAttempts = int.MaxValue,
                    Delay = TimeSpan.FromSeconds(3),
                    OnRetry = (i) =>
                    {
                        Console.WriteLine($"Kunne ikke forbinde til RabbitMQ med forsøg nr: {i.AttemptNumber}, prøver igen...");
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        public async Task<IConnection> GetConnectionAsync()
        {
            if (_connection != null && _connection.IsOpen)
            {
                return _connection;
            }
            else
            {
                _connection = await _pipeline.ExecuteAsync(async ct =>
                {
                    return await _factory.CreateConnectionAsync();
                });
                return _connection;
            }

        }
    }
}
