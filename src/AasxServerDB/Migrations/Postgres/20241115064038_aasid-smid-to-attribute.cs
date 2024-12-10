using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class aasidsmidtoattribute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AASIdShort",
                table: "SMESets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SMIdShort",
                table: "SMESets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AASIdShort",
                table: "SMESets");

            migrationBuilder.DropColumn(
                name: "SMIdShort",
                table: "SMESets");
        }
    }
}
