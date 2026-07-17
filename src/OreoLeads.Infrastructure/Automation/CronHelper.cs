namespace OreoLeads.Infrastructure.Automation;

/// <summary>
/// Minimal cron parser supporting 5 fields: minute hour day-of-month month day-of-week.
/// Supports *, */N, comma-separated values, and ranges (N-M).
/// </summary>
internal static class CronHelper
{
    public static DateTime? GetNextOccurrence(string cron, DateTime from)
    {
        if (string.IsNullOrWhiteSpace(cron)) return null;

        var parts = cron.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5) return null;

        try
        {
            var minutes = ParseField(parts[0], 0, 59);
            var hours = ParseField(parts[1], 0, 23);
            var daysOfMonth = ParseField(parts[2], 1, 31);
            var months = ParseField(parts[3], 1, 12);
            var daysOfWeek = ParseField(parts[4], 0, 6);

            // Start searching from the next minute
            var candidate = from.AddMinutes(1);
            candidate = new DateTime(candidate.Year, candidate.Month, candidate.Day, candidate.Hour, candidate.Minute, 0, DateTimeKind.Utc);

            // Search up to 2 years ahead
            var limit = from.AddYears(2);

            while (candidate < limit)
            {
                if (months.Contains(candidate.Month) &&
                    daysOfMonth.Contains(candidate.Day) &&
                    daysOfWeek.Contains((int)candidate.DayOfWeek) &&
                    hours.Contains(candidate.Hour) &&
                    minutes.Contains(candidate.Minute))
                {
                    return candidate;
                }

                candidate = candidate.AddMinutes(1);

                // Optimization: skip ahead if month doesn't match
                if (!months.Contains(candidate.Month))
                {
                    candidate = new DateTime(candidate.Year, candidate.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
                    continue;
                }

                // Skip ahead if day doesn't match
                if (!daysOfMonth.Contains(candidate.Day) || !daysOfWeek.Contains((int)candidate.DayOfWeek))
                {
                    if (candidate.Hour != 0 || candidate.Minute != 0)
                    {
                        candidate = new DateTime(candidate.Year, candidate.Month, candidate.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
                    }
                    else
                    {
                        candidate = candidate.AddDays(1);
                    }
                    continue;
                }

                // Skip ahead if hour doesn't match
                if (!hours.Contains(candidate.Hour))
                {
                    candidate = new DateTime(candidate.Year, candidate.Month, candidate.Day, candidate.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
                }
            }

            return null;
        }
        catch
        {
            // If parsing fails, fallback: return +1 hour
            return from.AddHours(1);
        }
    }

    private static HashSet<int> ParseField(string field, int min, int max)
    {
        var result = new HashSet<int>();

        if (field == "*")
        {
            for (var i = min; i <= max; i++) result.Add(i);
            return result;
        }

        foreach (var part in field.Split(','))
        {
            if (part.Contains('/'))
            {
                var stepParts = part.Split('/');
                var start = stepParts[0] == "*" ? min : int.Parse(stepParts[0]);
                var step = int.Parse(stepParts[1]);
                for (var i = start; i <= max; i += step) result.Add(i);
            }
            else if (part.Contains('-'))
            {
                var rangeParts = part.Split('-');
                var from = int.Parse(rangeParts[0]);
                var to = int.Parse(rangeParts[1]);
                for (var i = from; i <= to; i++) result.Add(i);
            }
            else
            {
                result.Add(int.Parse(part));
            }
        }

        return result;
    }
}
