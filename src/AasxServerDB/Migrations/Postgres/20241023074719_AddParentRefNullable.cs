using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddParentRefNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AASSets_AASSets_ParentId",
                table: "AASSets");

            migrationBuilder.DropForeignKey(
                name: "FK_SMSets_SMSets_ParentId",
                table: "SMSets");

            migrationBuilder.AlterColumn<int>(
                name: "ParentId",
                table: "SMSets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ParentId",
                table: "AASSets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_AASSets_AASSets_ParentId",
                table: "AASSets",
                column: "ParentId",
                principalTable: "AASSets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SMSets_SMSets_ParentId",
                table: "SMSets",
                column: "ParentId",
                principalTable: "SMSets",
                principalColumn: "Id");
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

            migrationBuilder.AlterColumn<int>(
                name: "ParentId",
                table: "SMSets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ParentId",
                table: "AASSets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

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
    }
}
