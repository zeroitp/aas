using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Linq;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class updatesmejsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<JObject>(
                name: "SubmodelElement",
                table: "SMESets",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(object),
                oldType: "jsonb",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<object>(
                name: "SubmodelElement",
                table: "SMESets",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(JObject),
                oldType: "jsonb");
        }
    }
}
