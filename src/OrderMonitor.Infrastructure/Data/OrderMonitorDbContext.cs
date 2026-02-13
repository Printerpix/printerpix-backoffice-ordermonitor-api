using Microsoft.EntityFrameworkCore;
using OrderMonitor.Infrastructure.Data.Entities;

namespace OrderMonitor.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for the Order Monitor system.
/// Supports SQL Server, MySQL, and PostgreSQL providers.
/// </summary>
public class OrderMonitorDbContext : DbContext
{
    public OrderMonitorDbContext(DbContextOptions<OrderMonitorDbContext> options)
        : base(options)
    {
    }

    public DbSet<ConsolidationOrderEntity> ConsolidationOrders => Set<ConsolidationOrderEntity>();
    public DbSet<OrderProductTrackingEntity> OrderProductTrackings => Set<OrderProductTrackingEntity>();
    public DbSet<TrackingStatusEntity> TrackingStatuses => Set<TrackingStatusEntity>();
    public DbSet<SnSpecificationEntity> SnSpecifications => Set<SnSpecificationEntity>();
    public DbSet<MajorProductTypeEntity> MajorProductTypes => Set<MajorProductTypeEntity>();
    public DbSet<PartnerEntity> Partners => Set<PartnerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ConsolidationOrder
        modelBuilder.Entity<ConsolidationOrderEntity>(entity =>
        {
            entity.HasKey(e => e.CONumber);
            entity.HasMany(e => e.OrderProductTrackings)
                .WithOne(e => e.ConsolidationOrder)
                .HasForeignKey(e => e.CONumber)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // OrderProductTracking
        modelBuilder.Entity<OrderProductTrackingEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.TrackingStatus)
                .WithMany()
                .HasForeignKey(e => e.Status)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.SnSpecification)
                .WithMany()
                .HasForeignKey(e => e.OptSnSpId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Partner)
                .WithMany()
                .HasForeignKey(e => e.TPartnerCode)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(e => e.CONumber);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsPrimaryComponent);
            entity.HasIndex(e => e.OrderDate);
        });

        // TrackingStatus
        modelBuilder.Entity<TrackingStatusEntity>(entity =>
        {
            entity.HasKey(e => e.TrackingStatusId);
        });

        // SnSpecification
        modelBuilder.Entity<SnSpecificationEntity>(entity =>
        {
            entity.HasKey(e => e.SnId);

            entity.HasOne(e => e.MajorProductType)
                .WithMany()
                .HasForeignKey(e => e.MasterProductTypeId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // MajorProductType
        modelBuilder.Entity<MajorProductTypeEntity>(entity =>
        {
            entity.HasKey(e => e.MProductTypeId);
        });

        // Partner
        modelBuilder.Entity<PartnerEntity>(entity =>
        {
            entity.HasKey(e => e.PartnerId);
        });
    }
}
