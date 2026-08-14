using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OreoLeads.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invite_codes",
                columns: table => new
                {
                    id         = table.Column<Guid>(type: "uuid", nullable: false),
                    code       = table.Column<string>(type: "text", nullable: false),
                    note       = table.Column<string>(type: "text", nullable: true),
                    is_used    = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    used_by_email = table.Column<string>(type: "text", nullable: true),
                    used_at    = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invite_codes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_invite_codes_code",
                table: "invite_codes",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "invite_codes");
        }
    }
}
