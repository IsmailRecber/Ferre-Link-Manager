using Ferre.LinkManager.Domain.Links;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ferre.LinkManager.Infrastructure.Persistence.Configurations;

public class LinkConfiguration : IEntityTypeConfiguration<Link>
{
    public void Configure(EntityTypeBuilder<Link> builder)
    {
        builder.ToTable("Links");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Title)
            .HasMaxLength(Link.MaxTitleLength)
            .IsRequired();

        builder.Property(l => l.Url)
            .HasConversion(url => url.Value, value => LinkUrl.Create(value))
            .HasMaxLength(LinkUrl.MaxLength)
            .IsRequired();

        builder.Property(l => l.CategoryId);
        builder.Property(l => l.IsActive);
        builder.Property(l => l.IsDeleted);
        builder.Property(l => l.CreatedOnUtc);

        builder.Ignore(l => l.DomainEvents);

        builder.HasMany(l => l.RoleAccesses)
            .WithOne()
            .HasForeignKey(access => access.LinkId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(l => l.RoleAccesses)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(l => !l.IsDeleted);

        builder.HasIndex(l => l.CategoryId);
    }
}