using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class updateschemafortemplatechange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AttributeTemplateId",
                table: "SMESets",
                newName: "TemplateId");

            migrationBuilder.AddColumn<string>(
                name: "TemplateId",
                table: "SMSets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateId",
                table: "AASSets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "SMSets");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "AASSets");

            migrationBuilder.RenameColumn(
                name: "TemplateId",
                table: "SMESets",
                newName: "AttributeTemplateId");
        }
    }
}
