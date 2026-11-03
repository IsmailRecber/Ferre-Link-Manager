using Ferre.LinkManager.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ferre.LinkManager.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name)
            .HasConversion(name => name.Value, value => CategoryName.Create(value))
            .HasMaxLength(CategoryName.MaxLength)
            .IsRequired();

        builder.Property(c => c.IsActive);
        builder.Property(c => c.IsDeleted);
        builder.Property(c => c.CreatedOnUtc);

        builder.Ignore(c => c.DomainEvents);

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}