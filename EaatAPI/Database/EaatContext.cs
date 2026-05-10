using Global.Models;
using Microsoft.EntityFrameworkCore;

namespace EaatAPI.Database
{
    public class EaatContext : DbContext
    {
        public EaatContext(DbContextOptions options) : base(options)
        {
        }

        public EaatContext()
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Data Source=DESKTOP-7O1GS44\\SQLEXPRESS;Initial Catalog=EaatEksamen;Integrated Security=True;Trust Server Certificate=True");
            //optionsBuilder.UseSqlServer("Data Source=LocalHost\\SQLEXPRESS;Initial Catalog=EaatEksamen;Integrated Security=True;Trust Server Certificate=True");
        }

        public DbSet<Kunde> Kunder { get; set; }
        public DbSet<Bud> Buds { get; set; }
        public DbSet<Restaurant> Restauranter { get; set; }
        public DbSet<Bestilling> Bestillinger { get; set; }

    }
}

