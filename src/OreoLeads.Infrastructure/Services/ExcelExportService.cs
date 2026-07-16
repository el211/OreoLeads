using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Leads.DTOs;

namespace OreoLeads.Infrastructure.Services;

public class ExcelExportService : IExcelExportService
{
    public Task<byte[]> ExportLeadsToCsvAsync(List<LeadSummaryDto> leads, CancellationToken ct = default)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Id,Entreprise,Nom commercial,Secteur,Ville,Département,Région,Email,Téléphone,Site,Statut,Priorité,Score,Tags,Créé le,Modifié le");

        foreach (var lead in leads)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(lead.Id.ToString()),
                EscapeCsv(lead.CompanyName),
                EscapeCsv(lead.TradeName ?? ""),
                EscapeCsv(lead.Industry ?? ""),
                EscapeCsv(lead.City ?? ""),
                EscapeCsv(lead.Department ?? ""),
                EscapeCsv(lead.Region ?? ""),
                EscapeCsv(lead.Email ?? ""),
                EscapeCsv(lead.Phone ?? ""),
                EscapeCsv(lead.Website ?? ""),
                EscapeCsv(lead.StatusLabel),
                EscapeCsv(lead.PriorityLabel),
                lead.Score.ToString(),
                EscapeCsv(string.Join(";", lead.Tags.Select(t => t.Name))),
                EscapeCsv(lead.CreatedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)),
                EscapeCsv(lead.UpdatedAt?.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) ?? "")
            ));
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    public Task<byte[]> ExportLeadsToExcelAsync(List<LeadSummaryDto> leads, CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Prospects");

        // Headers
        string[] headers = [
            "Id", "Entreprise", "Nom commercial", "Secteur", "Ville",
            "Département", "Région", "Email", "Téléphone", "Site web",
            "Statut", "Priorité", "Score", "Tags", "Créé le", "Modifié le"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Data rows
        for (int row = 0; row < leads.Count; row++)
        {
            var lead = leads[row];
            var rowNum = row + 2;

            ws.Cell(rowNum, 1).Value = lead.Id.ToString();
            ws.Cell(rowNum, 2).Value = lead.CompanyName;
            ws.Cell(rowNum, 3).Value = lead.TradeName ?? "";
            ws.Cell(rowNum, 4).Value = lead.Industry ?? "";
            ws.Cell(rowNum, 5).Value = lead.City ?? "";
            ws.Cell(rowNum, 6).Value = lead.Department ?? "";
            ws.Cell(rowNum, 7).Value = lead.Region ?? "";
            ws.Cell(rowNum, 8).Value = lead.Email ?? "";
            ws.Cell(rowNum, 9).Value = lead.Phone ?? "";
            ws.Cell(rowNum, 10).Value = lead.Website ?? "";
            ws.Cell(rowNum, 11).Value = lead.StatusLabel;
            ws.Cell(rowNum, 12).Value = lead.PriorityLabel;
            ws.Cell(rowNum, 13).Value = lead.Score;
            ws.Cell(rowNum, 14).Value = string.Join(", ", lead.Tags.Select(t => t.Name));
            ws.Cell(rowNum, 15).Value = lead.CreatedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            ws.Cell(rowNum, 16).Value = lead.UpdatedAt?.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) ?? "";
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
