using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OreoLeads.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiEmailSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneratedAt",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "OpenedAt",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "generated_emails");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "generated_emails",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "generated_emails",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Generated",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Draft");

            migrationBuilder.AddColumn<string>(
                name: "CallToAction",
                table: "generated_emails",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletionTokens",
                table: "generated_emails",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentVersion",
                table: "generated_emails",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GenerationMs",
                table: "generated_emails",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Length",
                table: "generated_emails",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModelUsed",
                table: "generated_emails",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PromptTokens",
                table: "generated_emails",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProviderUsed",
                table: "generated_emails",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Style",
                table: "generated_emails",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "generated_emails",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalTokens",
                table: "generated_emails",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "generated_emails",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ai_configurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Claude"),
                    EncryptedApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Temperature = table.Column<float>(type: "real", nullable: false),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_configurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "email_draft_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailDraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ProviderUsed = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModelUsed = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GenerationMs = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_draft_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_email_draft_versions_generated_emails_EmailDraftId",
                        column: x => x.EmailDraftId,
                        principalTable: "generated_emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prompt_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmailType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_templates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_generated_emails_CreatedAt",
                table: "generated_emails",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_generated_emails_Status",
                table: "generated_emails",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_email_draft_versions_EmailDraftId",
                table: "email_draft_versions",
                column: "EmailDraftId");

            migrationBuilder.CreateIndex(
                name: "IX_email_draft_versions_EmailDraftId_Version",
                table: "email_draft_versions",
                columns: new[] { "EmailDraftId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prompt_templates_Key",
                table: "prompt_templates",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_configurations");

            migrationBuilder.DropTable(
                name: "email_draft_versions");

            migrationBuilder.DropTable(
                name: "prompt_templates");

            migrationBuilder.DropIndex(
                name: "IX_generated_emails_CreatedAt",
                table: "generated_emails");

            migrationBuilder.DropIndex(
                name: "IX_generated_emails_Status",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "CallToAction",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "CompletionTokens",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "CurrentVersion",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "GenerationMs",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "ModelUsed",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "PromptTokens",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "ProviderUsed",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "Style",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "TotalTokens",
                table: "generated_emails");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "generated_emails");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "generated_emails",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "generated_emails",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Draft",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Generated");

            migrationBuilder.AddColumn<DateTime>(
                name: "GeneratedAt",
                table: "generated_emails",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "OpenedAt",
                table: "generated_emails",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentAt",
                table: "generated_emails",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
