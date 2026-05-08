using EaatAPI.Database;
using EaatAPI.Models;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text.Json;

namespace EaatAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EaatController : ControllerBase
    {
        private EaatContext _context;
        private readonly IConnection _connection;


        public EaatController(EaatContext context, IConnection connection)
        {
            _context = context;
            _connection = connection;
        }

        [HttpGet("kunder")]
        public IEnumerable<Kunde> GetKunder()
        {
            return _context.Kunder.ToList();
        }

        [HttpPost("kunder")]
        public void PostKunde(Kunde kunde)
        {
            _context.Kunder.Add(kunde);
            _context.SaveChanges();
        }

        [HttpGet("restauranter")]
        public IEnumerable<Restaurant> GetRestauranter()
        {
            return _context.Restauranter.ToList();
        }
        [HttpPost("restauranter")]
        public void PostRestaurant(Restaurant restaurant)
        {
            _context.Restauranter.Add(restaurant);
            _context.SaveChanges();
        }

        [HttpGet("bestillinger")]
        public IEnumerable<Bestilling> GetBestillinger()
        {
            return _context.Bestillinger.Where(b => b.BudId == null || b.BudId == 0).ToList();
        }
        [HttpPost("bestillinger")]
        public async Task PostBestilling(Bestilling bestilling)
        {
            _context.Bestillinger.Add(bestilling);
            _context.SaveChanges();
            var message = JsonSerializer.Serialize(bestilling);
            var body = System.Text.Encoding.UTF8.GetBytes(message);
            var channel = await _connection.CreateChannelAsync();
            await channel.BasicPublishAsync(exchange: "bestillingerTilBud", routingKey: string.Empty, body: body);
        }

        [HttpGet("buds")]
        public IEnumerable<Bud> GetBuds()
        {
            return _context.Buds.ToList();
        }
        [HttpPost("buds")]
        public void PostBud(Bud bud)
        {
            _context.Buds.Add(bud);
            _context.SaveChanges();
        }
    }
}
