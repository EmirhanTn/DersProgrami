using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DersProgrami.Data.Migrations
{
    /// <inheritdoc />
    public partial class ScheduleUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_TeacherId",
                table: "Schedules");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TeacherId_Day_Hour",
                table: "Schedules",
                columns: new[] { "TeacherId", "Day", "Hour" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_TeacherId_Day_Hour",
                table: "Schedules");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TeacherId",
                table: "Schedules",
                column: "TeacherId");
        }
    }
}
