using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class addsmemetadatacolumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AliasPath",
                table: "SMESets");

            migrationBuilder.RenameColumn(
                name: "SubmodelElement",
                table: "SMESets",
                newName: "RawJson");

            migrationBuilder.AddColumn<string>(
                name: "MetaData",
                table: "SMESets",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MetaData",
                table: "SMESets");

            migrationBuilder.RenameColumn(
                name: "RawJson",
                table: "SMESets",
                newName: "SubmodelElement");

            migrationBuilder.AddColumn<string>(
                name: "AliasPath",
                table: "SMESets",
                type: "text",
                nullable: true);
        }
    }
}
