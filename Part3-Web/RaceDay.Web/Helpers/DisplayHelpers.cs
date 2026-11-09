namespace RaceDay.Web.Helpers;

public static class DisplayHelpers
{
    public static string Emoji(this string? eventType) => eventType switch
    {
        "Run" => "🏃",
        "Walk" => "🚶",
        "Cycle" => "🚴",
        _ => "🏁"
    };

    public static string Ordinal(int number)
    {
        var suffix = number % 100 is >= 11 and <= 13
            ? "th"
            : (number % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return $"{number}{suffix}";
    }

    public static string Time(this TimeSpan? time) =>
        time.HasValue ? time.Value.ToString(@"hh\:mm\:ss") : "-";

    public static string Money(this decimal amount) => "R" + amount.ToString("0.00");

    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..1].ToUpperInvariant()
            : (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }
}
