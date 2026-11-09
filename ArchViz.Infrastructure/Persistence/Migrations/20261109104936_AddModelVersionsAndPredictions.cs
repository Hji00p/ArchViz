using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArchViz.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModelVersionsAndPredictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MLModelVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Version = table.Column<string>(type: "text", nullable: false),
                    TrainedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Mae = table.Column<double>(type: "double precision", nullable: false),
                    Rmse = table.Column<double>(type: "double precision", nullable: false),
                    ModelPath = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MLModelVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MLPredictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssessmentId = table.Column<int>(type: "integer", nullable: false),
                    ModelVersionId = table.Column<int>(type: "integer", nullable: false),
                    PredictedHours = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MLPredictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MLPredictions_MLModelVersions_ModelVersionId",
                        column: x => x.ModelVersionId,
                        principalTable: "MLModelVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MLPredictions_SceneAssessment_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "SceneAssessment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MLModelVersions_Version",
                table: "MLModelVersions",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MLPredictions_AssessmentId_ModelVersionId",
                table: "MLPredictions",
                columns: new[] { "AssessmentId", "ModelVersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MLPredictions_ModelVersionId",
                table: "MLPredictions",
                column: "ModelVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MLPredictions");

            migrationBuilder.DropTable(
                name: "MLModelVersions");
        }
    }
}
