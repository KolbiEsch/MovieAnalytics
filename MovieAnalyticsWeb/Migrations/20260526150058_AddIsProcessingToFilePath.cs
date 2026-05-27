using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieAnalyticsWeb.Migrations
{
    public partial class AddIsProcessingToFilePath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProcessing",
                table: "FilePaths",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsProcessing",
                table: "FilePaths");
        }
    }
}
