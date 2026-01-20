using Microsoft.EntityFrameworkCore;
using StockViewer.Core.Entities;

namespace StockViewer.Core.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Stock> Stocks { get; set; }
    public DbSet<DailyAttention> DailyAttentions { get; set; }
    public DbSet<DispositionRecord> DispositionRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DailyAttention>()
            .HasOne(d => d.Stock)
            .WithMany()
            .HasForeignKey(d => d.StockCode);

        modelBuilder.Entity<DispositionRecord>()
            .HasOne(d => d.Stock)
            .WithMany()
            .HasForeignKey(d => d.StockCode);
    }
}
