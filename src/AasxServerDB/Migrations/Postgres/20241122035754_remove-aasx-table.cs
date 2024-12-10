using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class removeaasxtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AASSets_AASXSets_AASXSetId",
                table: "AASSets");

            migrationBuilder.DropForeignKey(
                name: "FK_SMSets_AASXSets_AASXSetId",
                table: "SMSets");

            migrationBuilder.DropTable(
                name: "SMESnapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AASXSets",
                table: "AASXSets");

            migrationBuilder.RenameTable(
                name: "AASXSets",
                newName: "AASXSet");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AASXSet",
                table: "AASXSet",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AASSets_AASXSet_AASXSetId",
                table: "AASSets",
                column: "AASXSetId",
                principalTable: "AASXSet",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SMSets_AASXSet_AASXSetId",
                table: "SMSets",
                column: "AASXSetId",
                principalTable: "AASXSet",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AASSets_AASXSet_AASXSetId",
                table: "AASSets");

            migrationBuilder.DropForeignKey(
                name: "FK_SMSets_AASXSet_AASXSetId",
                table: "SMSets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AASXSet",
                table: "AASXSet");

            migrationBuilder.RenameTable(
                name: "AASXSet",
                newName: "AASXSets");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AASXSets",
                table: "AASXSets",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "SMESnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AASId = table.Column<int>(type: "integer", nullable: false),
                    SMEId = table.Column<int>(type: "integer", nullable: false),
                    AASIdShort = table.Column<Guid>(type: "uuid", nullable: false),
                    SMEIdshort = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMESnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SMESnapshots_AASSets_AASId",
                        column: x => x.AASId,
                        principalTable: "AASSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SMESnapshots_SMESets_SMEId",
                        column: x => x.SMEId,
                        principalTable: "SMESets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SMESnapshots_AASId",
                table: "SMESnapshots",
                column: "AASId");

            migrationBuilder.CreateIndex(
                name: "IX_SMESnapshots_SMEId",
                table: "SMESnapshots",
                column: "SMEId");

            migrationBuilder.AddForeignKey(
                name: "FK_AASSets_AASXSets_AASXSetId",
                table: "AASSets",
                column: "AASXSetId",
                principalTable: "AASXSets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SMSets_AASXSets_AASXSetId",
                table: "SMSets",
                column: "AASXSetId",
                principalTable: "AASXSets",
                principalColumn: "Id");
        }
    }
}
