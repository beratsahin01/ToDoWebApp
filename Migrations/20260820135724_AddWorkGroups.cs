using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToDoWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryShares");

            migrationBuilder.AddColumn<int>(
                name: "WorkGroupId",
                table: "TaskItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkGroupId",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkGroups_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkGroupMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkGroupId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkGroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkGroupMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkGroupMembers_WorkGroups_WorkGroupId",
                        column: x => x.WorkGroupId,
                        principalTable: "WorkGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_WorkGroupId",
                table: "TaskItems",
                column: "WorkGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_WorkGroupId",
                table: "Categories",
                column: "WorkGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkGroupMembers_UserId",
                table: "WorkGroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkGroupMembers_WorkGroupId",
                table: "WorkGroupMembers",
                column: "WorkGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkGroups_OwnerId",
                table: "WorkGroups",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_WorkGroups_WorkGroupId",
                table: "Categories",
                column: "WorkGroupId",
                principalTable: "WorkGroups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_WorkGroups_WorkGroupId",
                table: "TaskItems",
                column: "WorkGroupId",
                principalTable: "WorkGroups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_WorkGroups_WorkGroupId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_WorkGroups_WorkGroupId",
                table: "TaskItems");

            migrationBuilder.DropTable(
                name: "WorkGroupMembers");

            migrationBuilder.DropTable(
                name: "WorkGroups");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_WorkGroupId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_Categories_WorkGroupId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "WorkGroupId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "WorkGroupId",
                table: "Categories");

            migrationBuilder.CreateTable(
                name: "CategoryShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryShares_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryShares_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryShares_CategoryId",
                table: "CategoryShares",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryShares_UserId",
                table: "CategoryShares",
                column: "UserId");
        }
    }
}
