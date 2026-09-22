namespace MarketplaceAdvisory.SharedKernel.Tenancy;

/// <summary>
/// Strongly-typed tenant identifier. Every aggregate and every request is scoped by it.
/// </summary>
public readonly record struct TenantId(Guid Value)
{
    public static TenantId New() => new(Guid.NewGuid());

    public static TenantId From(Guid value) => new(value);

    public static bool TryParse(string? value, out TenantId tenantId)
    {
        if (Guid.TryParse(value, out var guid))
        {
            tenantId = new TenantId(guid);
            return true;
        }

        tenantId = default;
        return false;
    }

    public override string ToString() => Value.ToString();
}
