using Jadlify.Domain.Shopping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jadlify.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the shopping-list aggregate. Items and their sources are modelled as regular child
/// entities keyed by their own <c>Guid</c> rather than owned collections: a refresh removes some
/// rows and adds others in one save, and owned-collection change tracking reuses rows across a
/// delete+add, which corrupts the result. Regular entities keyed by id make each remove a delete
/// and each add an insert, deterministically. They are still reached only through the root — no
/// <c>DbSet</c> — so the aggregate boundary holds.
/// </summary>
internal sealed class ShoppingListConfiguration : IEntityTypeConfiguration<ShoppingList>
{
    public void Configure(EntityTypeBuilder<ShoppingList> builder)
    {
        builder.ToTable("shopping_lists");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.Name).HasColumnName("name").HasMaxLength(200).IsRequired();

        builder.Property(l => l.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // A SHA-256 hex digest is always 64 characters; sizing the column to it keeps the
        // fingerprint a fixed, indexable string rather than unbounded text.
        builder.Property(l => l.SourceFingerprint)
            .HasColumnName("source_fingerprint")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(l => l.Version).HasColumnName("version");
        builder.Property(l => l.CreatedAt).HasColumnName("created_at");
        builder.Property(l => l.CompletedAt).HasColumnName("completed_at");

        builder.Property<string>(PersistenceConstants.UserIdProperty)
            .HasColumnName(PersistenceConstants.UserIdColumn)
            .IsRequired();

        // History listing filters by owner and status; the composite index serves it directly.
        builder.HasIndex(PersistenceConstants.UserIdProperty, nameof(ShoppingList.Status))
            .HasDatabaseName("ix_shopping_lists_user_id_status");

        // At most one active list per user, enforced in the database as the backstop to the
        // handler's check. A partial unique index lets any number of completed lists coexist.
        builder.HasIndex(PersistenceConstants.UserIdProperty)
            .IsUnique()
            .HasFilter($"status = '{nameof(ShoppingListStatus.Active)}'")
            .HasDatabaseName("ux_shopping_lists_active_per_user");

        // The selected source days never change after creation, so an owned collection is a good
        // fit: a simple value keyed by (list, date), reached only through the root.
        builder.OwnsMany(l => l.SourceDays, day =>
        {
            day.ToTable("shopping_list_source_days");
            day.WithOwner().HasForeignKey("ShoppingListId");
            day.Property<Guid>("ShoppingListId").HasColumnName("shopping_list_id");
            day.Property(d => d.Date).HasColumnName("date");
            day.HasKey("ShoppingListId", nameof(ShoppingListSourceDay.Date));
        });

        builder.HasMany(l => l.Items)
            .WithOne()
            .HasForeignKey("ShoppingListId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ShoppingListItemConfiguration : IEntityTypeConfiguration<ShoppingListItem>
{
    public void Configure(EntityTypeBuilder<ShoppingListItem> builder)
    {
        builder.ToTable("shopping_list_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property<Guid>("ShoppingListId").HasColumnName("shopping_list_id");

        // product_id is a grouping key, not a foreign key: the snapshot must survive the catalog
        // product being renamed, recategorised, or deleted, exactly as recipe ingredients do.
        builder.Property(i => i.ProductId).HasColumnName("product_id");
        builder.Property(i => i.ProductName).HasColumnName("product_name").HasMaxLength(200).IsRequired();
        builder.Property(i => i.Category)
            .HasColumnName("product_category")
            .HasConversion<string>()
            .HasMaxLength(40);
        builder.Property(i => i.Grams).HasColumnName("grams").HasPrecision(12, 3);
        builder.Property(i => i.IsBought).HasColumnName("is_bought");

        builder.HasIndex("ShoppingListId").HasDatabaseName("ix_shopping_list_items_shopping_list_id");
        builder.HasIndex(i => i.ProductId).HasDatabaseName("ix_shopping_list_items_product_id");

        builder.HasMany(i => i.Sources)
            .WithOne()
            .HasForeignKey("ShoppingListItemId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ShoppingListItemSourceConfiguration : IEntityTypeConfiguration<ShoppingListItemSource>
{
    public void Configure(EntityTypeBuilder<ShoppingListItemSource> builder)
    {
        builder.ToTable("shopping_list_item_sources");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property<Guid>("ShoppingListItemId").HasColumnName("shopping_list_item_id");

        builder.Property(s => s.Date).HasColumnName("date");
        builder.Property(s => s.MealType)
            .HasColumnName("meal_type")
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(s => s.SourceLabel).HasColumnName("source_label").HasMaxLength(200).IsRequired();
        builder.Property(s => s.Grams).HasColumnName("grams").HasPrecision(12, 3);

        builder.HasIndex("ShoppingListItemId")
            .HasDatabaseName("ix_shopping_list_item_sources_shopping_list_item_id");
    }
}
