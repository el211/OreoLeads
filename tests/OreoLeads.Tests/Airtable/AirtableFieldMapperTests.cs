using FluentAssertions;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Airtable;

namespace OreoLeads.Tests.Airtable;

public class AirtableFieldMapperTests
{
    // ── 1. MapLeadToFields_AllMappedFields_ReturnsCorrectValues ──────────────

    [Fact]
    public void MapLeadToFields_AllMappedFields_ReturnsCorrectValues()
    {
        var lead = new Lead
        {
            CompanyName = "ACME Corp",
            Email       = "contact@acme.com",
            Phone       = "+33123456789",
            City        = "Paris",
            Country     = "France",
            Score       = 85,
        };

        var mappings = new List<AirtableFieldMapping>
        {
            new() { OreoLeadsField = "CompanyName", AirtableFieldName = "Company",  AirtableFieldType = AirtableFieldType.SingleLineText, Direction = SyncDirection.OreoLeadsToAirtable },
            new() { OreoLeadsField = "Email",       AirtableFieldName = "Email",    AirtableFieldType = AirtableFieldType.Email,           Direction = SyncDirection.OreoLeadsToAirtable },
            new() { OreoLeadsField = "Score",       AirtableFieldName = "Score",    AirtableFieldType = AirtableFieldType.Number,          Direction = SyncDirection.OreoLeadsToAirtable },
        };

        var result = AirtableFieldMapper.MapLeadToAirtableFields(lead, mappings);

        result.Should().ContainKey("Company").WhoseValue.Should().Be("ACME Corp");
        result.Should().ContainKey("Email").WhoseValue.Should().Be("contact@acme.com");
        result.Should().ContainKey("Score");
    }

    // ── 2. MapLeadToFields_EmailField_MapsEmail ───────────────────────────────

    [Fact]
    public void MapLeadToFields_EmailField_MapsEmail()
    {
        var lead = new Lead { CompanyName = "Test", Email = "test@example.com" };

        var mappings = new List<AirtableFieldMapping>
        {
            new() { OreoLeadsField = "Email", AirtableFieldName = "Email Address",
                    AirtableFieldType = AirtableFieldType.Email, Direction = SyncDirection.Bidirectional }
        };

        var result = AirtableFieldMapper.MapLeadToAirtableFields(lead, mappings);

        result["Email Address"].Should().Be("test@example.com");
    }

    // ── 3. MapLeadToFields_StatusField_MapsStatus ────────────────────────────

    [Fact]
    public void MapLeadToFields_StatusField_MapsStatus()
    {
        var lead = new Lead { CompanyName = "Test", Status = LeadStatus.Qualified };

        var mappings = new List<AirtableFieldMapping>
        {
            new() { OreoLeadsField = "Status", AirtableFieldName = "Status",
                    AirtableFieldType = AirtableFieldType.SingleSelect, Direction = SyncDirection.OreoLeadsToAirtable }
        };

        var result = AirtableFieldMapper.MapLeadToAirtableFields(lead, mappings);

        result["Status"].Should().Be("Qualified");
    }

    // ── 4. MapFieldsToLead_UpdatesLeadFields ─────────────────────────────────

    [Fact]
    public void MapFieldsToLead_UpdatesLeadFields()
    {
        var lead   = new Lead { CompanyName = "Old Company" };
        var fields = new Dictionary<string, object?> { ["Company"] = "New Company", ["Email"] = "new@company.com" };

        var mappings = new List<AirtableFieldMapping>
        {
            new() { AirtableFieldName = "Company", OreoLeadsField = "CompanyName",
                    AirtableFieldType = AirtableFieldType.SingleLineText, Direction = SyncDirection.AirtableToOreoLeads },
            new() { AirtableFieldName = "Email",   OreoLeadsField = "Email",
                    AirtableFieldType = AirtableFieldType.Email, Direction = SyncDirection.Bidirectional },
        };

        AirtableFieldMapper.MapAirtableFieldsToLead(fields, mappings, lead);

        lead.CompanyName.Should().Be("New Company");
        lead.Email.Should().Be("new@company.com");
    }

    // ── 5. ComputeHash_SameData_SameHash ─────────────────────────────────────

    [Fact]
    public void ComputeHash_SameData_SameHash()
    {
        var fields1 = new Dictionary<string, object?> { ["Name"] = "ACME", ["Email"] = "a@b.com" };
        var fields2 = new Dictionary<string, object?> { ["Email"] = "a@b.com", ["Name"] = "ACME" };

        var hash1 = AirtableFieldMapper.ComputeHash(fields1);
        var hash2 = AirtableFieldMapper.ComputeHash(fields2);

        hash1.Should().Be(hash2);
    }

    // ── 6. ComputeHash_DifferentData_DifferentHash ───────────────────────────

    [Fact]
    public void ComputeHash_DifferentData_DifferentHash()
    {
        var fields1 = new Dictionary<string, object?> { ["Name"] = "ACME" };
        var fields2 = new Dictionary<string, object?> { ["Name"] = "BETA" };

        var hash1 = AirtableFieldMapper.ComputeHash(fields1);
        var hash2 = AirtableFieldMapper.ComputeHash(fields2);

        hash1.Should().NotBe(hash2);
    }

    // ── 7. GetOreoLeadsFieldValue_InvalidField_ReturnsNull ───────────────────

    [Fact]
    public void GetOreoLeadsFieldValue_InvalidField_ReturnsNull()
    {
        var lead = new Lead { CompanyName = "Test" };
        var value = AirtableFieldMapper.GetOreoLeadsFieldValue(lead, "NonExistentField123");
        value.Should().BeNull();
    }

    // ── 8. MapLeadToFields_EmptyMappings_ReturnsEmpty ─────────────────────────

    [Fact]
    public void MapLeadToFields_EmptyMappings_ReturnsEmpty()
    {
        var lead   = new Lead { CompanyName = "Test", Email = "x@y.com" };
        var result = AirtableFieldMapper.MapLeadToAirtableFields(lead, new List<AirtableFieldMapping>());
        result.Should().BeEmpty();
    }
}
