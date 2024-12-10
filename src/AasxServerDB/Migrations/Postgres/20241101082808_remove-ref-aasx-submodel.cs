using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class removerefaasxsubmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SMSets_AASXSets_AASXId",
                table: "SMSets");

            migrationBuilder.DropIndex(
                name: "IX_SMSets_AASXId",
                table: "SMSets");

            migrationBuilder.AlterColumn<int>(
                name: "AASXId",
                table: "SMSets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "AASXSetId",
                table: "SMSets",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_AASXSetId",
                table: "SMSets",
                column: "AASXSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_SMSets_AASXSets_AASXSetId",
                table: "SMSets",
                column: "AASXSetId",
                principalTable: "AASXSets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SMSets_AASXSets_AASXSetId",
                table: "SMSets");

            migrationBuilder.DropIndex(
                name: "IX_SMSets_AASXSetId",
                table: "SMSets");

            migrationBuilder.DropColumn(
                name: "AASXSetId",
                table: "SMSets");

            migrationBuilder.AlterColumn<int>(
                name: "AASXId",
                table: "SMSets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_AASXId",
                table: "SMSets",
                column: "AASXId");

            migrationBuilder.AddForeignKey(
                name: "FK_SMSets_AASXSets_AASXId",
                table: "SMSets",
                column: "AASXId",
                principalTable: "AASXSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
