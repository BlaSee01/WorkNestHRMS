using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkNestHRMS.Migrations
{
    /// <inheritdoc />
    public partial class AddResponseContentToTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResponseContent",
                table: "Tasks",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponseContent",
                table: "Tasks");
        }
    }
}
