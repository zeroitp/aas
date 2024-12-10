using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class addoverridetemplateidattribute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttributeTemplateId",
                table: "SMESets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOverridden",
                table: "SMESets",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttributeTemplateId",
                table: "SMESets");

            migrationBuilder.DropColumn(
                name: "IsOverridden",
                table: "SMESets");
        }
    }
}
