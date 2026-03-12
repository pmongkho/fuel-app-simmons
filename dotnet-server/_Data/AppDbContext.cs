using dotnet_server._Models;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server._Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<FuelEntry> FuelEntries => Set<FuelEntry>();
}
