using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArchViz.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScenePlanApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScenePlanApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PredictionId = table.Column<int>(type: "integer", nullable: false),
                    ApprovedById = table.Column<string>(type: "text", nullable: false),
                    ApprovedHours = table.Column<decimal>(type: "numeric", nullable: false),
                    AdjustmentReason = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenePlanApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenePlanApprovals_AspNetUsers_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScenePlanApprovals_MLPredictions_PredictionId",
                        column: x => x.PredictionId,
                        principalTable: "MLPredictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScenePlanApprovals_ApprovedById",
                table: "ScenePlanApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_ScenePlanApprovals_PredictionId",
                table: "ScenePlanApprovals",
                column: "PredictionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScenePlanApprovals");
        }
    }
}
