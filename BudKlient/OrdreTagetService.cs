using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace BudKlient
{
    public class OrdreTagetService : IAsyncDisposable
    {
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly ConnectionFactory _factory;

        public OrdreTagetService()
        {
            _factory = new ConnectionFactory() { HostName = "localhost" };
        }

        // Hjælpemetode til at sikre at vi kun bygger connection/channel én gang.
        private async Task InitializeRabbitMQAsync()
        {
            if (_connection == null || _channel == null)
            {
                _connection = await _factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                // Det er nok at deklarere din exchange én gang ved opstart
                await _channel.ExchangeDeclareAsync(exchange: "bestillingerFraBud", type: ExchangeType.Direct, durable: true);
            }
        }

        public async Task SendTagetBeskedAsync(int bestillingId, int budId)
        {
            await InitializeRabbitMQAsync();

            var opdatering = new { Id = bestillingId, BudId = budId };
            var message = JsonSerializer.Serialize(opdatering);
            var body = Encoding.UTF8.GetBytes(message);

            // Garanterer at _channel ikke er null
            if (_channel != null)
            {
                await _channel.BasicPublishAsync(exchange: "bestillingerFraBud", routingKey: String.Empty, body: body);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel != null)
            {
                await _channel.DisposeAsync();
            }
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
        }
    }
}