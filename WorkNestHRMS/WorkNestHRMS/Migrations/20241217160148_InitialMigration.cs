using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WorkNestHRMS.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserWorkplaces",
                table: "UserWorkplaces");

            migrationBuilder.DropIndex(
                name: "IX_UserWorkplaces_UserId",
                table: "UserWorkplaces");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Workplaces");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "UserWorkplaces");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "UserWorkplaces");

            migrationBuilder.RenameColumn(
                name: "Industry",
                table: "Workplaces",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "UserWorkplaces",
                newName: "Role");

            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "Workplaces",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserWorkplaces",
                table: "UserWorkplaces",
                columns: new[] { "UserId", "WorkplaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Workplaces_OwnerId",
                table: "Workplaces",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Workplaces_Users_OwnerId",
                table: "Workplaces",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workplaces_Users_OwnerId",
                table: "Workplaces");

            migrationBuilder.DropIndex(
                name: "IX_Workplaces_OwnerId",
                table: "Workplaces");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserWorkplaces",
                table: "UserWorkplaces");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Workplaces");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Workplaces",
                newName: "Industry");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "UserWorkplaces",
                newName: "Status");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Workplaces",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "UserWorkplaces",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "UserWorkplaces",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserWorkplaces",
                table: "UserWorkplaces",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserWorkplaces_UserId",
                table: "UserWorkplaces",
                column: "UserId");
        }
    }
}
