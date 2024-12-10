using AasxServerDB.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class ModelForJsonB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<JSubmodel>(
                name: "Submodel",
                table: "SMSets",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "SMESets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<JAssetAdministrationShell>(
                name: "AssetAdministrationShell",
                table: "AASSets",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "AASSets",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "SMESets");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "AASSets");

            migrationBuilder.AlterColumn<string>(
                name: "Submodel",
                table: "SMSets",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(JSubmodel),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "AssetAdministrationShell",
                table: "AASSets",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(JAssetAdministrationShell),
                oldType: "jsonb");
        }
    }
}
