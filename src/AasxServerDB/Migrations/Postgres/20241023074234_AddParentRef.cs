using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddParentRef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "SMSets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "AASSets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_ParentId",
                table: "SMSets",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_AASSets_ParentId",
                table: "AASSets",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AASSets_AASSets_ParentId",
                table: "AASSets",
                column: "ParentId",
                principalTable: "AASSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SMSets_SMSets_ParentId",
                table: "SMSets",
                column: "ParentId",
                principalTable: "SMSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AASSets_AASSets_ParentId",
                table: "AASSets");

            migrationBuilder.DropForeignKey(
                name: "FK_SMSets_SMSets_ParentId",
                table: "SMSets");

            migrationBuilder.DropIndex(
                name: "IX_SMSets_ParentId",
                table: "SMSets");

            migrationBuilder.DropIndex(
                name: "IX_AASSets_ParentId",
                table: "AASSets");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "SMSets");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "AASSets");
        }
    }
}
