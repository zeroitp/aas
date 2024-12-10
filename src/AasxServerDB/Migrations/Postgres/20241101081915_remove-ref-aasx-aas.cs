using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class removerefaasxaas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AASSets_AASXSets_AASXId",
                table: "AASSets");

            migrationBuilder.DropIndex(
                name: "IX_AASSets_AASXId",
                table: "AASSets");

            migrationBuilder.AlterColumn<int>(
                name: "AASXId",
                table: "AASSets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "AASXSetId",
                table: "AASSets",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AASSets_AASXSetId",
                table: "AASSets",
                column: "AASXSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_AASSets_AASXSets_AASXSetId",
                table: "AASSets",
                column: "AASXSetId",
                principalTable: "AASXSets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AASSets_AASXSets_AASXSetId",
                table: "AASSets");

            migrationBuilder.DropIndex(
                name: "IX_AASSets_AASXSetId",
                table: "AASSets");

            migrationBuilder.DropColumn(
                name: "AASXSetId",
                table: "AASSets");

            migrationBuilder.AlterColumn<int>(
                name: "AASXId",
                table: "AASSets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AASSets_AASXId",
                table: "AASSets",
                column: "AASXId");

            migrationBuilder.AddForeignKey(
                name: "FK_AASSets_AASXSets_AASXId",
                table: "AASSets",
                column: "AASXId",
                principalTable: "AASXSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
