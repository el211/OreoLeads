using System.Net;
using System.Text.RegularExpressions;

namespace OreoLeads.Infrastructure.Enrichment;

public static class PhoneExtractor
{
    public static string? Normalize(string value)
    {
        value = WebUtility.HtmlDecode(Uri.UnescapeDataString(value)).Trim();
        if (value.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)) value = value[4..];
        value = value.Replace("(0)", "");
        if (!Regex.IsMatch(value, @"^[+\d\s().-]+$", RegexOptions.None, TimeSpan.FromMilliseconds(100))) return null;
        var digits = new string(value.Where(char.IsAsciiDigit).ToArray());
        if (digits.StartsWith("0033")) digits = digits[2..];
        if (digits.Length == 10 && digits[0] == '0') digits = "33" + digits[1..];
        return digits.Length == 11 && digits.StartsWith("33") && digits[2] is >= '1' and <= '9'
            ? "+" + digits : null;
    }

    public static bool IsFrenchMobile(string phone) => phone.StartsWith("+336") || phone.StartsWith("+337");

    public static string[] Extract(string html)
    {
        var values = new List<string>();
        foreach (Match m in Regex.Matches(html, "(?:href\\s*=\\s*[\"']tel:|[\"']telephone[\"']\\s*:\\s*[\"'])(?<phone>[^\"'<>]+)", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200)))
            values.Add(m.Groups["phone"].Value);
        // Exclude scripts and continuous identifiers when examining visible text.
        var text = Regex.Replace(html, @"<script\b[^>]*>.*?</script>|<style\b[^>]*>.*?</style>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromMilliseconds(200));
        text = WebUtility.HtmlDecode(Regex.Replace(text, "<[^>]+>", " ", RegexOptions.None, TimeSpan.FromMilliseconds(200)));
        foreach (Match m in Regex.Matches(text, @"(?<!\d)(?:0[1-9]|\+33\s?[1-9])(?:[ .-]\d{2}){4}(?!\d)", RegexOptions.None, TimeSpan.FromMilliseconds(200)))
            values.Add(m.Value);
        return values.Select(Normalize).OfType<string>().Distinct().Take(10).ToArray();
    }
}
