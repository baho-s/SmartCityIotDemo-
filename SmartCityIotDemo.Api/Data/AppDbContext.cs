using Microsoft.EntityFrameworkCore;
using SmartCityIotDemo.Api.Entities;

namespace SmartCityIotDemo.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<TelemetryData> TelemetryData => Set<TelemetryData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>()
            .HasIndex(x => x.DeviceCode)
            .IsUnique();

        modelBuilder.Entity<TelemetryData>()
            .HasIndex(x => x.DeviceCode);

        modelBuilder.Entity<TelemetryData>()
            .HasIndex(x => x.CreatedAt);//Her cihazın telemetri verilerini hızlı bir şekilde sorgulamak için CreatedAt alanına indeks eklenmiştir.
    }
}