using AasxServerDB.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class ModelNullableForJsonB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<JSubmodel>(
                name: "Submodel",
                table: "SMSets",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(JSubmodel),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SMESets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AASSets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<JAssetAdministrationShell>(
                name: "AssetAdministrationShell",
                table: "AASSets",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(JAssetAdministrationShell),
                oldType: "jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<JSubmodel>(
                name: "Submodel",
                table: "SMSets",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(JSubmodel),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SMESets",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AASSets",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<JAssetAdministrationShell>(
                name: "AssetAdministrationShell",
                table: "AASSets",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(JAssetAdministrationShell),
                oldType: "jsonb",
                oldNullable: true);
        }
    }
}
