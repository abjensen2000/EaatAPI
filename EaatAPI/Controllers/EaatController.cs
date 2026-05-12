using EaatAPI.Database;
using Global.Models;
using Global.Services;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text.Json;
using System.Threading.Channels;

namespace EaatAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EaatController : ControllerBase
    {
        private EaatContext _context;
        private ForbindTilRabbitService _forbindTilRabbitService;

        public EaatController(EaatContext context, ForbindTilRabbitService forbindTilRabbitService)
        {
            _context = context;
            _forbindTilRabbitService = forbindTilRabbitService;
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
            return _context.Bestillinger.Where(b => b.AccepteretAfRestaurant && (b.BudId == null || b.BudId == 0)).ToList();
        }
        [HttpPost("bestillinger")]
        public async Task PostBestilling(Bestilling bestilling)
        {
            _context.Bestillinger.Add(bestilling);
            _context.SaveChanges();
            var beskedTilKunde = new OutboxMessage("bestillingerFraAPITilRestaurant", bestilling.RestaurantId.ToString(), JsonSerializer.Serialize(bestilling));
            _context.OutboxMessages.AddRange(beskedTilKunde);
            _context.SaveChanges();

        }

        [HttpGet("bestillinger/restaurant/{restaurantId}")]
        public IEnumerable<Bestilling> GetBestillingerTilRestaurant(int restaurantId)
        {
            return _context.Bestillinger.Where(b => b.RestaurantId == restaurantId && !b.AccepteretAfRestaurant).ToList();
        }

        [HttpPut("bestillinger/{id}/accepterRestaurant")]
        public async Task<IActionResult> AccepterBestillingRestaurant(int id)
        {
            var bestilling = _context.Bestillinger.Find(id);
            if (bestilling == null) return NotFound();
            bestilling.AccepteretAfRestaurant = true;
            //var body = System.Text.Encoding.UTF8.GetBytes(message);

            var beskedTilBud = new OutboxMessage("bestillingerFraRestaurantTilBud", string.Empty, JsonSerializer.Serialize(bestilling));
            var beskedTilKunde = new OutboxMessage("notifikationTilKunde", bestilling.KundeId.ToString(), JsonSerializer.Serialize(bestilling));
            _context.OutboxMessages.AddRange(beskedTilBud, beskedTilKunde);
            _context.SaveChanges();

            return Ok();
        }

        [HttpPut("bestillinger/{bestillingId}/accepterBud/{budId}")]
        public async Task<IActionResult> TilknytBud(int bestillingId, int budId)
        {
            var bestilling = _context.Bestillinger.Find(bestillingId);

            if (bestilling == null)
            {
                return NotFound("Bestilling ikke fundet");
            }

            if (bestilling.BudId == budId)
            { //Er det her idempotent??
                return Ok();
            }

            if (bestilling.BudId != 0 && bestilling.BudId != null) //Race-condition??
            {
                return Conflict("Denne bestilling er allerede taget af et andet bud");
            }
            bestilling.BudId = budId;
            var beskedTilKunde = new OutboxMessage("notifikationTilKunde", bestilling.KundeId.ToString(), JsonSerializer.Serialize(bestilling));
            _context.OutboxMessages.AddRange(beskedTilKunde);
            _context.SaveChanges();


            return Ok();
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
