using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArchViz.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSceneAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SceneAssessment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequirementsRevisionId = table.Column<int>(type: "integer", nullable: false),
                    AssessedById = table.Column<string>(type: "text", nullable: false),
                    ViewsCount = table.Column<int>(type: "integer", nullable: false),
                    ModelingComplexity = table.Column<int>(type: "integer", nullable: false),
                    CustomModelingRatio = table.Column<decimal>(type: "numeric", nullable: false),
                    MaterialsComplexity = table.Column<int>(type: "integer", nullable: false),
                    LightingComplexity = table.Column<int>(type: "integer", nullable: false),
                    PostProcessingComplexity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SceneAssessment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SceneAssessment_AspNetUsers_AssessedById",
                        column: x => x.AssessedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SceneAssessment_SceneRequirementsRevisions_RequirementsRevi~",
                        column: x => x.RequirementsRevisionId,
                        principalTable: "SceneRequirementsRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SceneAssessment_AssessedById",
                table: "SceneAssessment",
                column: "AssessedById");

            migrationBuilder.CreateIndex(
                name: "IX_SceneAssessment_RequirementsRevisionId",
                table: "SceneAssessment",
                column: "RequirementsRevisionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SceneAssessment");
        }
    }
}
