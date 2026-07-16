using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OreoLeads.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchQuery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "search_queries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Keywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NafCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ActiveOnly = table.Column<bool>(type: "boolean", nullable: false),
                    MaxResults = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "OpenDataGouv"),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    TotalFound = table.Column<int>(type: "integer", nullable: false),
                    NewLeads = table.Column<int>(type: "integer", nullable: false),
                    UpdatedLeads = table.Column<int>(type: "integer", nullable: false),
                    Duplicates = table.Column<int>(type: "integer", nullable: false),
                    Errors = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Searched"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_search_queries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_search_queries_CreatedAt",
                table: "search_queries",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "search_queries");
        }
    }
}
