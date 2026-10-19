using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OreoLeads.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProspectingCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {



















            migrationBuilder.AddColumn<string>(
                name: "WebsiteDomain",
                table: "leads",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE leads SET "WebsiteDomain" = lower(rtrim(substring(trim("Website") from '(?i)^(?:https?://)?(?:www\.)?([a-z0-9.-]+)(?:[/:?#]|$)'), '.'))
                WHERE "Website" IS NOT NULL;
                """);

            migrationBuilder.AddColumn<string>(
                name: "InspectionJson",
                table: "LeadEnrichments",
                type: "text",
                nullable: true);


            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "company_contacts",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ObservedAt",
                table: "company_contacts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileUrl",
                table: "company_contacts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "company_contacts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationStatus",
                table: "company_contacts",
                type: "text",
                nullable: false,
                defaultValue: "Unknown");





            migrationBuilder.CreateTable(
                name: "ProspectingCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CriteriaJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DailyLimit = table.Column<int>(type: "integer", nullable: false),
                    DailyPageLimit = table.Column<int>(type: "integer", nullable: false),
                    MinimumScore = table.Column<int>(type: "integer", nullable: false),
                    NextPage = table.Column<int>(type: "integer", nullable: false),
                    PageOffset = table.Column<int>(type: "integer", nullable: false),
                    NextRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    QuotaDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DiscoveredToday = table.Column<int>(type: "integer", nullable: false),
                    PagesToday = table.Column<int>(type: "integer", nullable: false),
                    Failures = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProspectingCampaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CampaignExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Page = table.Column<int>(type: "integer", nullable: false),
                    Discovered = table.Column<int>(type: "integer", nullable: false),
                    Duplicates = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignExecutions_ProspectingCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "ProspectingCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampaignProspects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    EvidenceJson = table.Column<string>(type: "text", nullable: false),
                    QualifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignProspects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignProspects_ProspectingCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "ProspectingCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignProspects_leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_leads_OrganizationId_WebsiteDomain",
                table: "leads",
                columns: new[] { "OrganizationId", "WebsiteDomain" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignExecutions_CampaignId_CreatedAt",
                table: "CampaignExecutions",
                columns: new[] { "CampaignId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignProspects_CampaignId_LeadId",
                table: "CampaignProspects",
                columns: new[] { "CampaignId", "LeadId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignProspects_LeadId",
                table: "CampaignProspects",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_ProspectingCampaigns_Status_NextRunAt",
                table: "ProspectingCampaigns",
                columns: new[] { "Status", "NextRunAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignExecutions");

            migrationBuilder.DropTable(
                name: "CampaignProspects");

            migrationBuilder.DropTable(
                name: "ProspectingCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_leads_OrganizationId_WebsiteDomain",
                table: "leads");



            migrationBuilder.DropColumn(
                name: "WebsiteDomain",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "InspectionJson",
                table: "LeadEnrichments");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "company_contacts");

            migrationBuilder.DropColumn(
                name: "ObservedAt",
                table: "company_contacts");

            migrationBuilder.DropColumn(
                name: "ProfileUrl",
                table: "company_contacts");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "company_contacts");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "company_contacts");






















        }
    }
}
