using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities;

public class AirtableFieldMapping : BaseEntity
{
    public Guid AirtableConfigurationId { get; set; }
    public AirtableConfiguration? AirtableConfiguration { get; set; }
    public string OreoLeadsField { get; set; } = string.Empty;      // e.g. "Email"
    public string AirtableFieldName { get; set; } = string.Empty;   // e.g. "Email Address"
    public AirtableFieldType AirtableFieldType { get; set; } = AirtableFieldType.SingleLineText;
    public SyncDirection Direction { get; set; } = SyncDirection.Bidirectional;
    public bool IsRequired { get; set; } = false;
    public string? DefaultValue { get; set; }
    public string? Transformation { get; set; }
    public int SortOrder { get; set; } = 0;
}
