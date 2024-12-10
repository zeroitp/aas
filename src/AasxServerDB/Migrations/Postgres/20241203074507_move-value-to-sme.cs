using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class movevaluetosme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AASSets_AASXSet_AASXSetId",
                table: "AASSets");

            migrationBuilder.DropForeignKey(
                name: "FK_OValueSets_SMESets_SMEId",
                table: "OValueSets");

            migrationBuilder.DropForeignKey(
                name: "FK_SMSets_AASXSet_AASXSetId",
                table: "SMSets");

            migrationBuilder.DropTable(
                name: "AASXSet");

            migrationBuilder.DropTable(
                name: "DValueSets");

            migrationBuilder.DropTable(
                name: "IValueSets");

            migrationBuilder.DropTable(
                name: "SValueSets");

            migrationBuilder.DropIndex(
                name: "IX_SMSets_AASXSetId",
                table: "SMSets");

            migrationBuilder.DropIndex(
                name: "IX_AASSets_AASXSetId",
                table: "AASSets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OValueSets",
                table: "OValueSets");

            migrationBuilder.DropColumn(
                name: "AASXId",
                table: "SMSets");

            migrationBuilder.DropColumn(
                name: "AASXSetId",
                table: "SMSets");

            migrationBuilder.DropColumn(
                name: "AASXId",
                table: "AASSets");

            migrationBuilder.DropColumn(
                name: "AASXSetId",
                table: "AASSets");

            migrationBuilder.RenameTable(
                name: "OValueSets",
                newName: "OValueSet");

            migrationBuilder.RenameIndex(
                name: "IX_OValueSets_SMEId",
                table: "OValueSet",
                newName: "IX_OValueSet_SMEId");

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "SMESets",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OValueSet",
                table: "OValueSet",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OValueSet_SMESets_SMEId",
                table: "OValueSet",
                column: "SMEId",
                principalTable: "SMESets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OValueSet_SMESets_SMEId",
                table: "OValueSet");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OValueSet",
                table: "OValueSet");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "SMESets");

            migrationBuilder.RenameTable(
                name: "OValueSet",
                newName: "OValueSets");

            migrationBuilder.RenameIndex(
                name: "IX_OValueSet_SMEId",
                table: "OValueSets",
                newName: "IX_OValueSets_SMEId");

            migrationBuilder.AddColumn<int>(
                name: "AASXId",
                table: "SMSets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AASXSetId",
                table: "SMSets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AASXId",
                table: "AASSets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AASXSetId",
                table: "AASSets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OValueSets",
                table: "OValueSets",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AASXSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AASX = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AASXSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMEId = table.Column<int>(type: "integer", nullable: false),
                    Annotation = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DValueSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DValueSets_SMESets_SMEId",
                        column: x => x.SMEId,
                        principalTable: "SMESets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMEId = table.Column<int>(type: "integer", nullable: false),
                    Annotation = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IValueSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IValueSets_SMESets_SMEId",
                        column: x => x.SMEId,
                        principalTable: "SMESets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMEId = table.Column<int>(type: "integer", nullable: false),
                    Annotation = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SValueSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SValueSets_SMESets_SMEId",
                        column: x => x.SMEId,
                        principalTable: "SMESets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_AASXSetId",
                table: "SMSets",
                column: "AASXSetId");

            migrationBuilder.CreateIndex(
                name: "IX_AASSets_AASXSetId",
                table: "AASSets",
                column: "AASXSetId");

            migrationBuilder.CreateIndex(
                name: "IX_DValueSets_SMEId",
                table: "DValueSets",
                column: "SMEId");

            migrationBuilder.CreateIndex(
                name: "IX_IValueSets_SMEId",
                table: "IValueSets",
                column: "SMEId");

            migrationBuilder.CreateIndex(
                name: "IX_SValueSets_SMEId",
                table: "SValueSets",
                column: "SMEId");

            migrationBuilder.AddForeignKey(
                name: "FK_AASSets_AASXSet_AASXSetId",
                table: "AASSets",
                column: "AASXSetId",
                principalTable: "AASXSet",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OValueSets_SMESets_SMEId",
                table: "OValueSets",
                column: "SMEId",
                principalTable: "SMESets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SMSets_AASXSet_AASXSetId",
                table: "SMSets",
                column: "AASXSetId",
                principalTable: "AASXSet",
                principalColumn: "Id");
        }
    }
}
