using Microsoft.EntityFrameworkCore;

namespace Models;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TickerSlope>()
            .OwnsMany(c => c.SlopeResults, d =>
            {
                d.ToJson();
            });
        modelBuilder.Entity<SlopeSummary>()
            .OwnsOne(c => c.PeriodStart, d =>
            {
                d.ToJson();
            });
        modelBuilder.Entity<SlopeSummary>()
            .OwnsOne(c => c.PeriodEnd, d =>
            {
                d.ToJson();
            });
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<IndexComponent> IndexComponents { get; set; }
    public DbSet<PriceByDate> PriceByDate { get; set; }
    public DbSet<SelectedTicker> SelectedTickers { get; set; }
    public DbSet<TickerSlope> TickerSlopes { get; set; }
    public DbSet<SlopeSummary> SlopeSummaries { get; set; }
}