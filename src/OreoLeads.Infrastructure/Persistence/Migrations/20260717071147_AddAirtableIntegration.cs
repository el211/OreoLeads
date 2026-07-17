using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OreoLeads.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAirtableIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "airtable_configurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConnectionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EncryptedAccessToken = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BaseId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TableIdOrName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SyncDirection = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConflictStrategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WebhookId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WebhookCursor = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WebhookExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airtable_configurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "airtable_record_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    AirtableConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    AirtableRecordId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ConflictStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ConflictOreoLeadsData = table.Column<string>(type: "text", nullable: true),
                    ConflictAirtableData = table.Column<string>(type: "text", nullable: true),
                    ConflictDetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConflictResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConflictResolvedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AirtableModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airtable_record_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_airtable_record_links_leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "airtable_sync_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AirtableConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Direction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TriggerReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsFullSync = table.Column<bool>(type: "boolean", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeadFilter = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TotalRecords = table.Column<int>(type: "integer", nullable: false),
                    ProcessedRecords = table.Column<int>(type: "integer", nullable: false),
                    SuccessRecords = table.Column<int>(type: "integer", nullable: false),
                    FailedRecords = table.Column<int>(type: "integer", nullable: false),
                    ConflictRecords = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    AirtableOffset = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airtable_sync_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "brevo_configurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    EncryptedApiKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SenderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SenderEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReplyTo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TestMode = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TestModeEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DailyLimit = table.Column<int>(type: "integer", nullable: false, defaultValue: 300),
                    WebhookSecret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brevo_configurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "email_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailSendJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    MessageId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Details = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "email_send_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedEmailId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BrevoMessageId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ToEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ToName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Subject = table.Column<string>(type: "character varying(998)", maxLength: 998, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_send_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "unsubscribe_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    UnsubscribedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "webhook"),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unsubscribe_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "airtable_field_mappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AirtableConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OreoLeadsField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AirtableFieldName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AirtableFieldType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Direction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Transformation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airtable_field_mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_airtable_field_mappings_airtable_configurations_AirtableCon~",
                        column: x => x.AirtableConfigurationId,
                        principalTable: "airtable_configurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "airtable_sync_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AirtableSyncJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    AirtableRecordId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Details = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airtable_sync_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_airtable_sync_logs_airtable_sync_jobs_AirtableSyncJobId",
                        column: x => x.AirtableSyncJobId,
                        principalTable: "airtable_sync_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_airtable_field_mappings_AirtableConfigurationId",
                table: "airtable_field_mappings",
                column: "AirtableConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_airtable_record_links_AirtableRecordId",
                table: "airtable_record_links",
                column: "AirtableRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_airtable_record_links_LeadId_AirtableConfigurationId",
                table: "airtable_record_links",
                columns: new[] { "LeadId", "AirtableConfigurationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_airtable_sync_logs_AirtableSyncJobId",
                table: "airtable_sync_logs",
                column: "AirtableSyncJobId");

            migrationBuilder.CreateIndex(
                name: "ix_email_events_lead_occurred",
                table: "email_events",
                columns: new[] { "LeadId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "ix_email_events_message_id",
                table: "email_events",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "ix_email_events_send_job_id",
                table: "email_events",
                column: "EmailSendJobId");

            migrationBuilder.CreateIndex(
                name: "ix_email_send_jobs_brevo_message_id",
                table: "email_send_jobs",
                column: "BrevoMessageId");

            migrationBuilder.CreateIndex(
                name: "ix_email_send_jobs_generated_email_id",
                table: "email_send_jobs",
                column: "GeneratedEmailId");

            migrationBuilder.CreateIndex(
                name: "ix_email_send_jobs_lead_id",
                table: "email_send_jobs",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "ix_email_send_jobs_status_scheduled",
                table: "email_send_jobs",
                columns: new[] { "Status", "ScheduledAt", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "ix_unsubscribe_records_email_unique",
                table: "unsubscribe_records",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_unsubscribe_records_lead_id",
                table: "unsubscribe_records",
                column: "LeadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "airtable_field_mappings");

            migrationBuilder.DropTable(
                name: "airtable_record_links");

            migrationBuilder.DropTable(
                name: "airtable_sync_logs");

            migrationBuilder.DropTable(
                name: "brevo_configurations");

            migrationBuilder.DropTable(
                name: "email_events");

            migrationBuilder.DropTable(
                name: "email_send_jobs");

            migrationBuilder.DropTable(
                name: "unsubscribe_records");

            migrationBuilder.DropTable(
                name: "airtable_configurations");

            migrationBuilder.DropTable(
                name: "airtable_sync_jobs");
        }
    }
}
