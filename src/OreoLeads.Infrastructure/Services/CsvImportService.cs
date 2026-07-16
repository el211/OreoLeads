using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Leads.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Services;

public class CsvImportService : ICsvImportService
{
    // Map of possible header names to our field names
    private static readonly Dictionary<string, string> HeaderMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["company"] = "CompanyName",
        ["entreprise"] = "CompanyName",
        ["société"] = "CompanyName",
        ["societe"] = "CompanyName",
        ["nom"] = "CompanyName",
        ["raison sociale"] = "CompanyName",
        ["raisonsociale"] = "CompanyName",
        ["companyname"] = "CompanyName",
        ["email"] = "Email",
        ["mail"] = "Email",
        ["courriel"] = "Email",
        ["e-mail"] = "Email",
        ["téléphone"] = "Phone",
        ["telephone"] = "Phone",
        ["phone"] = "Phone",
        ["tel"] = "Phone",
        ["tél"] = "Phone",
        ["mobile"] = "Phone",
        ["ville"] = "City",
        ["city"] = "City",
        ["commune"] = "City",
        ["secteur"] = "Industry",
        ["industry"] = "Industry",
        ["activité"] = "Industry",
        ["activite"] = "Industry",
        ["domaine"] = "Industry",
        ["naf"] = "NafCode",
        ["nafcode"] = "NafCode",
        ["code naf"] = "NafCode",
        ["site"] = "Website",
        ["website"] = "Website",
        ["url"] = "Website",
        ["web"] = "Website",
        ["siteinternet"] = "Website",
        ["site internet"] = "Website",
        ["adresse"] = "Address",
        ["address"] = "Address",
        ["cp"] = "PostalCode",
        ["postalcode"] = "PostalCode",
        ["codepostal"] = "PostalCode",
        ["code postal"] = "PostalCode",
        ["siret"] = "Siret",
        ["siren"] = "Siren",
    };

    public async Task<List<ImportLeadDto>> ParseAsync(Stream stream, CancellationToken ct = default)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null,
            PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant()
        };

        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        var headers = csv.HeaderRecord ?? Array.Empty<string>();
        var columnMap = BuildColumnMap(headers);

        var results = new List<ImportLeadDto>();

        while (await csv.ReadAsync())
        {
            var dto = new ImportLeadDto();

            foreach (var (colIndex, fieldName) in columnMap)
            {
                var value = csv.GetField(colIndex)?.Trim();
                if (string.IsNullOrWhiteSpace(value)) continue;

                switch (fieldName)
                {
                    case "CompanyName": dto.CompanyName = value; break;
                    case "Email": dto.Email = value; break;
                    case "Phone": dto.Phone = value; break;
                    case "City": dto.City = value; break;
                    case "Industry": dto.Industry = value; break;
                    case "Website": dto.Website = value; break;
                    case "Address": dto.Address = value; break;
                    case "PostalCode": dto.PostalCode = value; break;
                    case "Siren": dto.Siren = value; break;
                    case "Siret": dto.Siret = value; break;
                    // NafCode not in ImportLeadDto — skip
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.CompanyName))
                results.Add(dto);
        }

        return results;
    }

    public Lead MapToLead(ImportLeadDto dto) => new()
    {
        CompanyName = dto.CompanyName ?? string.Empty,
        Email = dto.Email,
        Phone = dto.Phone,
        City = dto.City,
        Industry = dto.Industry,
        Website = dto.Website,
        Address = dto.Address,
        PostalCode = dto.PostalCode,
        Siren = dto.Siren,
        Siret = dto.Siret,
        Status = LeadStatus.New,
        Priority = LeadPriority.Medium
    };

    private static Dictionary<int, string> BuildColumnMap(string[] headers)
    {
        var map = new Dictionary<int, string>();
        for (int i = 0; i < headers.Length; i++)
        {
            var header = headers[i].Trim().ToLowerInvariant();
            if (HeaderMappings.TryGetValue(header, out var fieldName))
                map[i] = fieldName;
        }
        return map;
    }
}
