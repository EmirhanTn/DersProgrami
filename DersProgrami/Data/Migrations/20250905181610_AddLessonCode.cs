using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DersProgrami.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lessons_DepartmentId",
                table: "Lessons");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Lessons",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_DepartmentId_Code",
                table: "Lessons",
                columns: new[] { "DepartmentId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lessons_DepartmentId_Code",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Lessons");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_DepartmentId",
                table: "Lessons",
                column: "DepartmentId");
        }
    }
}
