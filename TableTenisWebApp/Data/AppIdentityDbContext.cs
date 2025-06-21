using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TableTenisWebApp.Models;

namespace TableTenisWebApp.Data;

public class AppIdentityDbContext : IdentityDbContext<ApplicationUser>
{
    public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
        : base(options) { }


    public DbSet<Player> Players => Set<Player>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<ApplicationUser>().ToTable("AspNetUsers");

        mb.Entity<Player>()
          .HasOne(p => p.ApplicationUser)
          .WithOne()
          .HasForeignKey<Player>(p => p.ApplicationUserId)
          .OnDelete(DeleteBehavior.SetNull);
    }
}
