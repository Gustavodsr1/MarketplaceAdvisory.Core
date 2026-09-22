namespace MarketplaceAdvisory.SharedKernel.Guards;

/// <summary>
/// Lightweight guard clauses for argument validation.
/// </summary>
public static class Guard
{
    public static T AgainstNull<T>(T? value, string parameterName) where T : class =>
        value ?? throw new ArgumentNullException(parameterName);

    public static string AgainstNullOrWhiteSpace(string? value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or whitespace.", parameterName)
            : value;
}
