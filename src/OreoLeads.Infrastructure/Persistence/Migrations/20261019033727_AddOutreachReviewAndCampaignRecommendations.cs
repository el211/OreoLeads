using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OreoLeads.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutreachReviewAndCampaignRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutomaticAiRecommendation",
                table: "ProspectingCampaigns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DiscoveryIntervalMinutes",
                table: "ProspectingCampaigns",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "MaxPendingEnrichments",
                table: "ProspectingCampaigns",
                type: "integer",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextDiscoveryAt",
                table: "ProspectingCampaigns",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<bool>(
                name: "EvidenceOnly",
                table: "generated_emails",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GenerationInstructions",
                table: "generated_emails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "generated_emails",
                type: "text",
                nullable: false,
                defaultValue: "fr");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresOutreachReview",
                table: "generated_emails",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AiAttempts",
                table: "CampaignProspects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Existing unsent marketing drafts must also pass eligibility review after upgrading.
            migrationBuilder.Sql("""
                UPDATE generated_emails SET "RequiresOutreachReview" = TRUE
                WHERE "Type" IN ('FirstContact', 'FollowUp', 'LastFollowUp') AND "Status" <> 'Sent';
                """);

            migrationBuilder.AddColumn<string>(
                name: "AiError",
                table: "CampaignProspects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AiLeaseToken",
                table: "CampaignProspects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AiLeaseUntil",
                table: "CampaignProspects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAiAttemptAt",
                table: "CampaignProspects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendationJson",
                table: "CampaignProspects",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OutreachReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailDraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientEmail = table.Column<string>(type: "text", nullable: false),
                    CountryCode = table.Column<string>(type: "text", nullable: false),
                    Basis = table.Column<string>(type: "text", nullable: false),
                    BasisEvidence = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SenderIdentification = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PrivacyNoticeUrl = table.Column<string>(type: "text", nullable: false),
                    OptOutUrl = table.Column<string>(type: "text", nullable: false),
                    ContentHash = table.Column<string>(type: "text", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "text", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedByUserId = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutreachReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutreachReviews_generated_emails_EmailDraftId",
                        column: x => x.EmailDraftId,
                        principalTable: "generated_emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutreachReviews_EmailDraftId",
                table: "OutreachReviews",
                column: "EmailDraftId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutreachReviews");

            migrationBuilder.DropColumn(
                name: "AutomaticAiRecommendation",
                table: "ProspectingCampaigns");

            migrationBuilder.DropColumn(
                name: "DiscoveryIntervalMinutes",
                table: "ProspectingCampaigns");

            migrationBuilder.DropColumn(
                name: "MaxPendingEnrichments",
                table: "ProspectingCampaigns");

            migrationBuilder.DropColumn(
                name: "NextDiscoveryAt",
                table: "ProspectingCampaigns");

            migrationBuilder.DropColumn(
                name: "EvidenceOnly",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "GenerationInstructions",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "RequiresOutreachReview",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "AiAttempts",
                table: "CampaignProspects");

            migrationBuilder.DropColumn(
                name: "AiError",
                table: "CampaignProspects");

            migrationBuilder.DropColumn(
                name: "AiLeaseToken",
                table: "CampaignProspects");

            migrationBuilder.DropColumn(
                name: "AiLeaseUntil",
                table: "CampaignProspects");

            migrationBuilder.DropColumn(
                name: "NextAiAttemptAt",
                table: "CampaignProspects");

            migrationBuilder.DropColumn(
                name: "RecommendationJson",
                table: "CampaignProspects");
        }
    }
}
