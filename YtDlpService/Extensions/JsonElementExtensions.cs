using System.Text.Json;

namespace YtDlpService.Extensions;

public static class JsonElementExtensions
{
    public static string? GetStringProperty(this JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;
    }

    public static int? GetInt32Property(this JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetInt32OrNull() : null;
    }

    public static long? GetInt64Property(this JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetInt64OrNull() : null;
    }

    public static int? GetInt32OrNull(this JsonElement element)
    {
        try
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number when element.TryGetInt32(out var i) => i,
                JsonValueKind.Number when element.TryGetDouble(out var d) => (int)d,
                JsonValueKind.String => int.TryParse(element.GetString(), out var s) ? s : null,
                _ => null
            };
        }
        catch { return null; }
    }

    public static long? GetInt64OrNull(this JsonElement element)
    {
        try
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number when element.TryGetInt64(out var l) => l,
                JsonValueKind.Number when element.TryGetDouble(out var d) => (long)d,
                JsonValueKind.String => long.TryParse(element.GetString(), out var s) ? s : null,
                _ => null
            };
        }
        catch { return null; }
    }

    public static string FormatDuration(int? seconds)
    {
        if (!seconds.HasValue) return "-";
        var ts = TimeSpan.FromSeconds(seconds.Value);
        return ts.Hours > 0
            ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes}:{ts.Seconds:D2}";
    }
}