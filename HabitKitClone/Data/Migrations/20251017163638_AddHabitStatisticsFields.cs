using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabitKitClone.Migrations
{
    /// <inheritdoc />
    public partial class AddHabitStatisticsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BestStreak",
                table: "Habits",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStreak",
                table: "Habits",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalCompletions",
                table: "Habits",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BestStreak",
                table: "Habits");

            migrationBuilder.DropColumn(
                name: "CurrentStreak",
                table: "Habits");

            migrationBuilder.DropColumn(
                name: "TotalCompletions",
                table: "Habits");
        }
    }
}
