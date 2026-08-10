namespace Jadlify.Domain.Shopping;

/// <summary>
/// A persistent shopping list: a named set of items aggregated from an arbitrary, bounded set
/// of plan days, shopped from until completed. It is the aggregate root — items, their source
/// contributions, and the selected days are reached only through it, so every change is one
/// owner-scoped, transactional write.
/// <para>
/// A user has at most one <see cref="ShoppingListStatus.Active"/> list at a time; completing it
/// freezes it into immutable history. <see cref="Version"/> is a monotonic counter bumped by the
/// structural changes (refresh, complete) so a caller can detect that the list moved on since it
/// last read it; <see cref="SourceFingerprint"/> captures the plan state the items represent so a
/// change in the underlying plan can be detected and previewed before it is applied.
/// </para>
/// </summary>
public sealed class ShoppingList
{
    private readonly List<ShoppingListSourceDay> _sourceDays = [];
    private readonly List<ShoppingListItem> _items = [];

    private ShoppingList()
    {
        // EF Core materialization constructor.
        Name = null!;
        SourceFingerprint = null!;
    }

    private ShoppingList(Guid id, string name, string sourceFingerprint, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Status = ShoppingListStatus.Active;
        SourceFingerprint = sourceFingerprint;
        Version = 1;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public ShoppingListStatus Status { get; private set; }

    /// <summary>Fingerprint of the plan projection the items currently represent.</summary>
    public string SourceFingerprint { get; private set; }

    /// <summary>Monotonic counter bumped by refresh and complete; the optimistic-concurrency token.</summary>
    public int Version { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyList<ShoppingListSourceDay> SourceDays => _sourceDays;

    public IReadOnlyList<ShoppingListItem> Items => _items;

    /// <summary>
    /// Creates a new active list from the selected <paramref name="days"/> and the projection of
    /// their meals. Days are stored distinct and ordered; the projection and its
    /// <paramref name="fingerprint"/> are computed together by the caller so the stored
    /// fingerprint matches the items.
    /// </summary>
    public static ShoppingList Create(
        Guid id,
        string name,
        IReadOnlyCollection<DateOnly> days,
        IReadOnlyList<ShoppingProjectionItem> projection,
        string fingerprint,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(days);
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);

        ShoppingList list = new(id, name.Trim(), fingerprint, createdAt);

        foreach (DateOnly day in days.Distinct().OrderBy(day => day))
        {
            list._sourceDays.Add(new ShoppingListSourceDay(day));
        }

        foreach (ShoppingProjectionItem projected in projection)
        {
            list._items.Add(ShoppingListItem.FromProjection(projected));
        }

        return list;
    }

    /// <summary>
    /// Ticks a single item bought or unbought. Returns <see langword="false"/> if no item with
    /// <paramref name="itemId"/> belongs to this list, so the caller can surface a 404 rather
    /// than silently succeeding. This is a per-item edit and deliberately does not bump
    /// <see cref="Version"/> — sequential ticks must not invalidate each other.
    /// </summary>
    public bool TrySetItemBought(Guid itemId, bool isBought)
    {
        EnsureActive();

        ShoppingListItem? item = _items.SingleOrDefault(item => item.Id == itemId);
        if (item is null)
        {
            return false;
        }

        item.SetBought(isBought);
        return true;
    }

    /// <summary>
    /// Computes what a refresh would change without touching the list. Used by the preview so
    /// the user confirms a concrete diff before anything is written.
    /// </summary>
    public ShoppingListDiff PreviewRefresh(IReadOnlyList<ShoppingProjectionItem> projection)
    {
        EnsureActive();

        return ShoppingListDiff.Between(_items, projection);
    }

    /// <summary>
    /// Applies a fresh projection of the same source days. Added and quantity-changed lines end
    /// up unbought; source-only changes keep their tick; unchanged lines are left alone; removed
    /// products drop out. The fingerprint is updated and <see cref="Version"/> is bumped so a
    /// stale preview elsewhere is rejected on its own confirmation.
    /// </summary>
    public void ApplyRefresh(IReadOnlyList<ShoppingProjectionItem> projection, string fingerprint)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);
        EnsureActive();

        var diff = ShoppingListDiff.Between(_items, projection);
        var projectionByProduct = projection.ToDictionary(item => item.ProductId);
        var itemsByProduct = _items.ToDictionary(item => item.ProductId);

        foreach (ShoppingListDiffLine line in diff.Removed)
        {
            _items.RemoveAll(item => item.ProductId == line.ProductId);
        }

        foreach (ShoppingListDiffLine line in diff.Changed)
        {
            itemsByProduct[line.ProductId].ApplyProjection(projectionByProduct[line.ProductId], resetBought: true);
        }

        foreach (ShoppingListDiffLine line in diff.SourceOnly)
        {
            itemsByProduct[line.ProductId].ApplyProjection(projectionByProduct[line.ProductId], resetBought: false);
        }

        foreach (ShoppingListDiffLine line in diff.Added)
        {
            _items.Add(ShoppingListItem.FromProjection(projectionByProduct[line.ProductId]));
        }

        SourceFingerprint = fingerprint;
        Version++;
    }

    /// <summary>Finishes the list: it becomes an immutable history entry. Bumps the version.</summary>
    public void Complete(DateTimeOffset completedAt)
    {
        EnsureActive();

        Status = ShoppingListStatus.Completed;
        CompletedAt = completedAt;
        Version++;
    }

    private void EnsureActive()
    {
        if (Status is not ShoppingListStatus.Active)
        {
            throw new InvalidOperationException("A completed shopping list is immutable.");
        }
    }
}
