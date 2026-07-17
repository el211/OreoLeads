using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Airtable;

internal static class AirtableFieldMapper
{
    // ── Lead → Airtable fields ────────────────────────────────────────────────

    public static Dictionary<string, object?> MapLeadToAirtableFields(
        Lead lead, List<AirtableFieldMapping> mappings)
    {
        var result = new Dictionary<string, object?>();

        foreach (var mapping in mappings.Where(m =>
            m.Direction == SyncDirection.OreoLeadsToAirtable ||
            m.Direction == SyncDirection.Bidirectional))
        {
            var value = GetOreoLeadsFieldValue(lead, mapping.OreoLeadsField);

            if (value is null && mapping.DefaultValue is not null)
                value = mapping.DefaultValue;

            result[mapping.AirtableFieldName] = SerializeForAirtable(value, mapping.AirtableFieldType);
        }

        return result;
    }

    // ── Airtable fields → Lead ────────────────────────────────────────────────

    public static void MapAirtableFieldsToLead(
        Dictionary<string, object?> fields, List<AirtableFieldMapping> mappings, Lead lead)
    {
        foreach (var mapping in mappings.Where(m =>
            m.Direction == SyncDirection.AirtableToOreoLeads ||
            m.Direction == SyncDirection.Bidirectional))
        {
            if (!fields.TryGetValue(mapping.AirtableFieldName, out var rawValue))
                continue;

            var value = DeserializeFromAirtable(rawValue, mapping.AirtableFieldType);
            SetOreoLeadsFieldValue(lead, mapping.OreoLeadsField, value);
        }
    }

    // ── Hash computation ──────────────────────────────────────────────────────

    public static string ComputeHash(Dictionary<string, object?> fields)
    {
        var sorted = new SortedDictionary<string, object?>(fields);
        var json   = JsonSerializer.Serialize(sorted);
        var bytes  = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ── Get field value from Lead ─────────────────────────────────────────────

    public static object? GetOreoLeadsFieldValue(Lead lead, string fieldName) => fieldName switch
    {
        "Id"          => lead.Id.ToString(),
        "FirstName"   => null,  // Lead doesn't have FirstName; return null
        "LastName"    => null,  // Lead doesn't have LastName; return null
        "CompanyName" => lead.CompanyName,
        "Email"       => lead.Email,
        "Phone"       => lead.Phone,
        "Website"     => lead.Website,
        "Address"     => lead.Address,
        "PostalCode"  => lead.PostalCode,
        "City"        => lead.City,
        "Country"     => lead.Country,
        "Industry"    => lead.Industry,
        "Status"      => lead.Status.ToString(),
        "Source"      => null,
        "Score"       => lead.Score,
        "Tags"        => null,  // requires navigation, handled externally
        "DoNotContact"=> lead.Status == LeadStatus.DoNotContact,
        "CreatedAt"   => lead.CreatedAt,
        "UpdatedAt"   => lead.UpdatedAt,
        "Siren"       => lead.Siren,
        "Siret"       => lead.Siret,
        _             => null,
    };

    // ── Set field value on Lead ───────────────────────────────────────────────

    public static void SetOreoLeadsFieldValue(Lead lead, string fieldName, object? value)
    {
        switch (fieldName)
        {
            case "CompanyName":
                if (value is string cn) lead.CompanyName = cn;
                break;
            case "Email":
                lead.Email = value?.ToString();
                break;
            case "Phone":
                lead.Phone = value?.ToString();
                break;
            case "Website":
                lead.Website = value?.ToString();
                break;
            case "Address":
                lead.Address = value?.ToString();
                break;
            case "PostalCode":
                lead.PostalCode = value?.ToString();
                break;
            case "City":
                lead.City = value?.ToString();
                break;
            case "Country":
                if (value is string country) lead.Country = country;
                break;
            case "Industry":
                lead.Industry = value?.ToString();
                break;
            case "Status":
                if (value is string statusStr &&
                    Enum.TryParse<LeadStatus>(statusStr, ignoreCase: true, out var parsedStatus))
                    lead.Status = parsedStatus;
                break;
            case "Score":
                if (value is int score) lead.Score = score;
                else if (value is string scoreStr && int.TryParse(scoreStr, out var parsedScore))
                    lead.Score = parsedScore;
                break;
            case "Siren":
                lead.Siren = value?.ToString();
                break;
            case "Siret":
                lead.Siret = value?.ToString();
                break;
        }
    }

    // ── Type conversion helpers ───────────────────────────────────────────────

    private static object? SerializeForAirtable(object? value, AirtableFieldType fieldType)
    {
        if (value is null) return null;

        return fieldType switch
        {
            AirtableFieldType.Number    => ConvertToDouble(value),
            AirtableFieldType.Checkbox  => ConvertToBool(value),
            AirtableFieldType.Date      => ConvertToDateString(value),
            AirtableFieldType.DateTime  => ConvertToDateTimeString(value),
            _                           => value?.ToString(),
        };
    }

    private static object? DeserializeFromAirtable(object? rawValue, AirtableFieldType fieldType)
    {
        if (rawValue is null) return null;

        var strVal = rawValue.ToString()?.Trim('"') ?? "";

        return fieldType switch
        {
            AirtableFieldType.Number   => double.TryParse(strVal, out var d) ? d : (object?)null,
            AirtableFieldType.Checkbox => strVal.ToLower() is "true" or "1",
            _                          => strVal,
        };
    }

    private static double? ConvertToDouble(object? value)
    {
        if (value is double d) return d;
        if (value is int i) return i;
        if (value is string s && double.TryParse(s, out var parsed)) return parsed;
        return null;
    }

    private static bool? ConvertToBool(object? value)
    {
        if (value is bool b) return b;
        if (value is string s) return s.ToLower() is "true" or "1" or "yes";
        return null;
    }

    private static string? ConvertToDateString(object? value)
    {
        if (value is DateTime dt) return dt.ToString("yyyy-MM-dd");
        return value?.ToString();
    }

    private static string? ConvertToDateTimeString(object? value)
    {
        if (value is DateTime dt) return dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        return value?.ToString();
    }
}
