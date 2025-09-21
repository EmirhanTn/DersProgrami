using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DersProgrami.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Teachers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Teachers");
        }
    }
}
