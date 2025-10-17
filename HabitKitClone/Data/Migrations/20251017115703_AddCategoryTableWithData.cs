using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabitKitClone.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryTableWithData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, create the Categories table
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false, defaultValue: "#3B82F6"),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false, defaultValue: "📝"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create a default "General" category for each user
            migrationBuilder.Sql(@"
                INSERT INTO Categories (Name, Color, Icon, IsActive, CreatedAt, UserId)
                SELECT 'General', '#3B82F6', '📝', 1, datetime('now'), Id
                FROM AspNetUsers;
            ");

            // Add CategoryId column to Habits table
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Habits",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Update existing habits to use the default "General" category
            migrationBuilder.Sql(@"
                UPDATE Habits 
                SET CategoryId = (
                    SELECT c.Id 
                    FROM Categories c 
                    WHERE c.UserId = Habits.UserId AND c.Name = 'General'
                    LIMIT 1
                );
            ");

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Habits_CategoryId",
                table: "Habits",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_UserId",
                table: "Categories",
                column: "UserId");

            // Add foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_Habits_Categories_CategoryId",
                table: "Habits",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Habits_Categories_CategoryId",
                table: "Habits");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Habits_CategoryId",
                table: "Habits");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Habits");
        }
    }
}
