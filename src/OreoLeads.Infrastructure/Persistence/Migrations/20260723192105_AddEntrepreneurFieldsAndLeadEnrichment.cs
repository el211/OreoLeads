using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OreoLeads.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEntrepreneurFieldsAndLeadEnrichment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailValidatedAt",
                table: "leads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntrepreneurFirstName",
                table: "leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntrepreneurLastName",
                table: "leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsIndividualEntrepreneur",
                table: "leads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "WebsiteValidatedAt",
                table: "leads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LeadEnrichments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    WebsiteCandidatesJson = table.Column<string>(type: "text", nullable: true),
                    ChosenWebsiteUrl = table.Column<string>(type: "text", nullable: true),
                    WebsiteConfidence = table.Column<double>(type: "double precision", nullable: true),
                    MatchedSignalsJson = table.Column<string>(type: "text", nullable: true),
                    SocialProfilesJson = table.Column<string>(type: "text", nullable: true),
                    DiscoveredEmail = table.Column<string>(type: "text", nullable: true),
                    EmailSourceUrl = table.Column<string>(type: "text", nullable: true),
                    EmailSourceType = table.Column<string>(type: "text", nullable: true),
                    EmailKind = table.Column<int>(type: "integer", nullable: false),
                    EmailConfidence = table.Column<double>(type: "double precision", nullable: true),
                    GuessedEmail = table.Column<string>(type: "text", nullable: true),
                    SearchQueriesUsed = table.Column<int>(type: "integer", nullable: false),
                    AutoApplied = table.Column<bool>(type: "boolean", nullable: false),
                    ValidatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadEnrichments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadEnrichments_leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeadEnrichments_LeadId",
                table: "LeadEnrichments",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadEnrichments_Status_ScheduledAt",
                table: "LeadEnrichments",
                columns: new[] { "Status", "ScheduledAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeadEnrichments");

            migrationBuilder.DropColumn(
                name: "EmailValidatedAt",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "EntrepreneurFirstName",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "EntrepreneurLastName",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "IsIndividualEntrepreneur",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "WebsiteValidatedAt",
                table: "leads");
        }
    }
}
