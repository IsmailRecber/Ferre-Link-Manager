using Ferre.LinkManager.Domain.Links;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ferre.LinkManager.Infrastructure.Persistence.Configurations;

public class LinkRoleAccessConfiguration : IEntityTypeConfiguration<LinkRoleAccess>
{
    public void Configure(EntityTypeBuilder<LinkRoleAccess> builder)
    {
        builder.ToTable("LinkRoleAccesses");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.LinkId);

        builder.Property(a => a.RoleName)
            .HasMaxLength(LinkRoleAccess.MaxRoleNameLength)
            .IsRequired();

        builder.HasIndex(a => new { a.LinkId, a.RoleName }).IsUnique();
    }
}