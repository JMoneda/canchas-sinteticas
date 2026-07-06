using CanchasSinteticas.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CanchasSinteticas.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<FieldModel> Fields { get; set; }
    public DbSet<ReservationModel> Reservations { get; set; }
    public DbSet<NoShowModel> NoShows { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FieldModel>().ToTable("fields");
        modelBuilder.Entity<ReservationModel>().ToTable("reservations");
        modelBuilder.Entity<NoShowModel>().ToTable("no_shows");
    }
}
