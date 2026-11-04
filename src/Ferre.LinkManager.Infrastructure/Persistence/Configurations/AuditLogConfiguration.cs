using Ferre.LinkManager.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ferre.LinkManager.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Action).HasMaxLength(AuditLog.MaxActionLength).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(AuditLog.MaxEntityTypeLength).IsRequired();
        builder.Property(a => a.EntityId);
        builder.Property(a => a.EntityName).HasMaxLength(AuditLog.MaxEntityNameLength);
        builder.Property(a => a.EntityUrl).HasMaxLength(AuditLog.MaxEntityUrlLength);
        builder.Property(a => a.UserId);
        builder.Property(a => a.OccurredOnUtc);

        builder.HasIndex(a => a.OccurredOnUtc);
        builder.HasIndex(a => a.EntityId);
    }
}