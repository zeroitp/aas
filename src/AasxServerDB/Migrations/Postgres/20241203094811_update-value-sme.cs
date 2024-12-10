using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class updatevaluesme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OValueSet");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "SMESets",
                newName: "SValue");

            migrationBuilder.AddColumn<string>(
                name: "DValue",
                table: "SMESets",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataType",
                table: "SMESets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IValue",
                table: "SMESets",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OValue",
                table: "SMESets",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DValue",
                table: "SMESets");

            migrationBuilder.DropColumn(
                name: "DataType",
                table: "SMESets");

            migrationBuilder.DropColumn(
                name: "IValue",
                table: "SMESets");

            migrationBuilder.DropColumn(
                name: "OValue",
                table: "SMESets");

            migrationBuilder.RenameColumn(
                name: "SValue",
                table: "SMESets",
                newName: "Value");

            migrationBuilder.CreateTable(
                name: "OValueSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMEId = table.Column<int>(type: "integer", nullable: false),
                    Attribute = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OValueSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OValueSet_SMESets_SMEId",
                        column: x => x.SMEId,
                        principalTable: "SMESets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OValueSet_SMEId",
                table: "OValueSet",
                column: "SMEId");
        }
    }
}
