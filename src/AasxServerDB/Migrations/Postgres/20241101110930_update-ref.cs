using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class updateref : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SMSets_AASSets_AASSetId",
                table: "SMSets");

            migrationBuilder.DropIndex(
                name: "IX_SMSets_AASSetId",
                table: "SMSets");

            migrationBuilder.DropColumn(
                name: "AASSetId",
                table: "SMSets");

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_AASId",
                table: "SMSets",
                column: "AASId");

            migrationBuilder.AddForeignKey(
                name: "FK_SMSets_AASSets_AASId",
                table: "SMSets",
                column: "AASId",
                principalTable: "AASSets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SMSets_AASSets_AASId",
                table: "SMSets");

            migrationBuilder.DropIndex(
                name: "IX_SMSets_AASId",
                table: "SMSets");

            migrationBuilder.AddColumn<int>(
                name: "AASSetId",
                table: "SMSets",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_AASSetId",
                table: "SMSets",
                column: "AASSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_SMSets_AASSets_AASSetId",
                table: "SMSets",
                column: "AASSetId",
                principalTable: "AASSets",
                principalColumn: "Id");
        }
    }
}
