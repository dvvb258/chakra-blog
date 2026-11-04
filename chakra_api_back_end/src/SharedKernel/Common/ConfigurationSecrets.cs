namespace SharedKernel.Common;

public static class ConfigurationSecrets
{
    public static bool IsPlaceholder(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            || value.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("placeholder", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("DevelopmentOnly", StringComparison.OrdinalIgnoreCase)
            || value.Equals("sk-...", StringComparison.OrdinalIgnoreCase);
    }
}
