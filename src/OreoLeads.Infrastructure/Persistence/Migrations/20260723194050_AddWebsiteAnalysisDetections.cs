using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OreoLeads.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebsiteAnalysisDetections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AnalyzedWithBrowser",
                table: "website_analyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BookingProvider",
                table: "website_analyses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasMessenger",
                table: "website_analyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasNewsletterForm",
                table: "website_analyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasWhatsApp",
                table: "website_analyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalyzedWithBrowser",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "BookingProvider",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "HasMessenger",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "HasNewsletterForm",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "HasWhatsApp",
                table: "website_analyses");
        }
    }
}
