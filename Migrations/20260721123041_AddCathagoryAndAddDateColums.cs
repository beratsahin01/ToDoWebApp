using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToDoWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCathagoryAndAddDateColums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatDate",
                table: "TaskItems",
                newName: "CreateDate");

            migrationBuilder.RenameColumn(
                name: "Cathagor",
                table: "TaskItems",
                newName: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreateDate",
                table: "TaskItems",
                newName: "CreatDate");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "TaskItems",
                newName: "Cathagor");
        }
    }
}
