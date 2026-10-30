using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToDoWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCathagoryToTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cathagor",
                table: "TaskItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cathagor",
                table: "TaskItems");
        }
    }
}
