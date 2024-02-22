using Microsoft.EntityFrameworkCore;

namespace AspireDemo.ApiService.Database;

public class EventStoreDbContext(DbContextOptions<EventStoreDbContext> options) : DbContext(options)
{
    public DbSet<EventEntity> Events => Set<EventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventEntity>().ToTable("Events");
        modelBuilder.Entity<EventEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<EventEntity>()
            .HasIndex(x => x.AggregateId);
        modelBuilder.Entity<EventEntity>()
            .HasIndex(x => new {x.AggregateId, x.Version})
            .IsUnique();
        modelBuilder.Entity<EventEntity>()
            .Property(e => e.Timestamp);
    }
}
