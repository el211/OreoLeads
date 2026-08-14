using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OreoLeads.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnsureTablesExist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent — creates tables only if they were never created
            // (guards against cases where previous migrations were recorded
            // in __EFMigrationsHistory but the CREATE TABLE DDL never ran).
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS invite_codes (
                    id            uuid                        PRIMARY KEY,
                    code          text                        NOT NULL,
                    note          text,
                    is_used       boolean                     NOT NULL DEFAULT false,
                    used_by_email text,
                    used_at       timestamp with time zone,
                    expires_at    timestamp with time zone,
                    created_at    timestamp with time zone    NOT NULL,
                    updated_at    timestamp with time zone
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_invite_codes_code
                    ON invite_codes (code);

                CREATE TABLE IF NOT EXISTS chat_messages (
                    id          uuid                        PRIMARY KEY,
                    user_id     text                        NOT NULL,
                    author_name text                        NOT NULL,
                    content     text                        NOT NULL,
                    created_at  timestamp with time zone    NOT NULL,
                    updated_at  timestamp with time zone
                );

                CREATE INDEX IF NOT EXISTS ix_chat_messages_created_at
                    ON chat_messages (created_at);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // intentionally left empty — dropping is handled by the original migrations
        }
    }
}
