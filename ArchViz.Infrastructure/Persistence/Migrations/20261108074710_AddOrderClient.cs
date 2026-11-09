using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchViz.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Orders");
        }
    }
}
