using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OreoLeads.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWebsiteAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoadTimeMs",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "MetaTitle",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "website_analyses");

            migrationBuilder.RenameColumn(
                name: "TechStack",
                table: "website_analyses",
                newName: "Recommendations");

            migrationBuilder.RenameColumn(
                name: "Score",
                table: "website_analyses",
                newName: "ResponseTimeMs");

            migrationBuilder.RenameColumn(
                name: "HasSsl",
                table: "website_analyses",
                newName: "UsesHttps");

            migrationBuilder.RenameColumn(
                name: "HasSocialLinks",
                table: "website_analyses",
                newName: "HasViewport");

            migrationBuilder.RenameColumn(
                name: "HasContact",
                table: "website_analyses",
                newName: "HasQuoteForm");

            migrationBuilder.RenameColumn(
                name: "HasBlog",
                table: "website_analyses",
                newName: "HasPrivacyPolicy");

            migrationBuilder.RenameColumn(
                name: "AnalyzedAt",
                table: "website_analyses",
                newName: "LastAnalysis");

            migrationBuilder.AddColumn<string>(
                name: "AnalysisError",
                table: "website_analyses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessScore",
                table: "website_analyses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "CertificateValid",
                table: "website_analyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CmsDetected",
                table: "website_analyses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAddressVisible",
                table: "website_analyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasBookingSystem",
                table: "website_analyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasChatWidget",
                table: "website_analyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasContactForm",
                table: "website_analyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasEmailVisible",
                table: "website_analyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasLegalNotice",
                table: "website_analyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasPhoneVisible",
                table: "website_analyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "HttpStatus",
                table: "website_analyses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PageTitle",
                table: "website_analyses",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RedirectCount",
                table: "website_analyses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "website_analyses",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnologiesDetected",
                table: "website_analyses",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "website_analyses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_website_analyses_CreatedAt",
                table: "website_analyses",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_website_analyses_CreatedAt",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "AnalysisError",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "BusinessScore",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "CertificateValid",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "CmsDetected",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "HasAddressVisible",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "HasBookingSystem",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "HasChatWidget",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "HasContactForm",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "HasEmailVisible",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "HasLegalNotice",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "HasPhoneVisible",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "HttpStatus",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "PageTitle",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "RedirectCount",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "TechnologiesDetected",
                table: "website_analyses");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "website_analyses");

            migrationBuilder.RenameColumn(
                name: "UsesHttps",
                table: "website_analyses",
                newName: "HasSsl");

            migrationBuilder.RenameColumn(
                name: "ResponseTimeMs",
                table: "website_analyses",
                newName: "Score");

            migrationBuilder.RenameColumn(
                name: "Recommendations",
                table: "website_analyses",
                newName: "TechStack");

            migrationBuilder.RenameColumn(
                name: "LastAnalysis",
                table: "website_analyses",
                newName: "AnalyzedAt");

            migrationBuilder.RenameColumn(
                name: "HasViewport",
                table: "website_analyses",
                newName: "HasSocialLinks");

            migrationBuilder.RenameColumn(
                name: "HasQuoteForm",
                table: "website_analyses",
                newName: "HasContact");

            migrationBuilder.RenameColumn(
                name: "HasPrivacyPolicy",
                table: "website_analyses",
                newName: "HasBlog");

            migrationBuilder.AddColumn<int>(
                name: "LoadTimeMs",
                table: "website_analyses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitle",
                table: "website_analyses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "website_analyses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }
    }
}
