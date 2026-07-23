using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace OreoLeads.Infrastructure.Services;

/// <summary>
/// Ensures an email body is valid HTML before sending.
/// Generated emails are stored as plain text; sending them with a text/html
/// content type collapses line breaks. This converts plain text to paragraphs
/// with &lt;br&gt; line breaks, and leaves bodies that already contain HTML untouched.
/// </summary>
public static partial class EmailBodyFormatter
{
    [GeneratedRegex(@"<\s*(p|div|br|html|body|table|a|span|strong|em|ul|ol|li|h[1-6])[\s>/]", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTagRegex();

    public static string EnsureHtml(string body)
    {
        if (string.IsNullOrWhiteSpace(body) || HtmlTagRegex().IsMatch(body))
            return body;

        var paragraphs = body
            .Replace("\r\n", "\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        var sb = new StringBuilder();
        foreach (var paragraph in paragraphs)
        {
            var lines = paragraph
                .Split('\n')
                .Select(l => WebUtility.HtmlEncode(l.TrimEnd()));
            sb.Append("<p style=\"margin:0 0 1em 0;\">")
              .Append(string.Join("<br/>", lines))
              .Append("</p>");
        }

        return sb.ToString();
    }
}
