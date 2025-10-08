using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabitKitClone.Migrations
{
    /// <inheritdoc />
    public partial class newmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DailyReminderTime",
                table: "UserSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(TimeOnly),
                oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeOnly>(
                name: "DailyReminderTime",
                table: "UserSettings",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
