using Microsoft.EntityFrameworkCore;
using TableTenisWebApp.Models;


namespace TableTenisWebApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Player> Players => Set<Player>();
        public DbSet<Match> Matches => Set<Match>();



    }
}
