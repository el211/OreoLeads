using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OreoLeads.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id          = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id     = table.Column<string>(type: "text", nullable: false),
                    author_name = table.Column<string>(type: "text", nullable: false),
                    content     = table.Column<string>(type: "text", nullable: false),
                    created_at  = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_created_at",
                table: "chat_messages",
                column: "created_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "chat_messages");
        }
    }
}
