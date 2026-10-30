using Microsoft.EntityFrameworkCore;

namespace InlineTelegramBot;

public class MetroStationsDBContext : DbContext
{
    public DbSet<Station> Stations => Set<Station>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=metro.db");
    }
}
