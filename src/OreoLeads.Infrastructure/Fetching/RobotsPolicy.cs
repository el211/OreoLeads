using System.Text.RegularExpressions;

namespace OreoLeads.Infrastructure.Fetching;

public static class RobotsPolicy
{
    public static bool Allows(string text, string path)
    {
        var groups = new List<(List<string> Agents, List<(string Path, bool Allow)> Rules)>();
        var agents = new List<string>();
        var rules = new List<(string Path, bool Allow)>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Split('#')[0].Trim();
            var split = line.IndexOf(':');
            if (split < 0) continue;
            var key = line[..split].Trim().ToLowerInvariant();
            var value = line[(split + 1)..].Trim();
            if (key == "user-agent")
            {
                if (rules.Count > 0) { groups.Add((agents, rules)); agents = []; rules = []; }
                agents.Add(value.ToLowerInvariant());
            }
            else if (key is "allow" or "disallow" && agents.Count > 0 && value.Length > 0)
                rules.Add((value, key == "allow"));
        }
        groups.Add((agents, rules));
        var specific = groups.Where(g => g.Agents.Contains("oreoleads")).ToList();
        var selected = specific.Count > 0 ? specific : groups.Where(g => g.Agents.Contains("*")).ToList();
        var matches = selected.SelectMany(g => g.Rules).Where(r =>
        {
            var end = r.Path.EndsWith('$');
            var pattern = "^" + Regex.Escape(end ? r.Path[..^1] : r.Path).Replace("\\*", ".*") + (end ? "$" : "");
            return Regex.IsMatch(path, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        }).OrderByDescending(r => r.Path.Replace("*", "").Length).ThenByDescending(r => r.Allow).ToList();
        return matches.Count == 0 || matches[0].Allow;
    }
}
