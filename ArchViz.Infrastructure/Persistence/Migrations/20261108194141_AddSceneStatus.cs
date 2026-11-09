using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchViz.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSceneStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Scenes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Scenes");
        }
    }
}
