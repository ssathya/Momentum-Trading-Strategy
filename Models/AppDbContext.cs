using Microsoft.EntityFrameworkCore;

namespace Models;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<IndexComponent> IndexComponents { get; set; }
}