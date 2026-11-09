using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArchViz.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryAndOrderWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedAt",
                table: "Scenes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedById",
                table: "Scenes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndReason",
                table: "SceneAssignments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndedById",
                table: "SceneAssignments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    UploadedById = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_AspNetUsers_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attachments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderActionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedById = table.Column<string>(type: "text", nullable: false),
                    TargetManagerId = table.Column<string>(type: "text", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DecidedById = table.Column<string>(type: "text", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderActionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderActionRequests_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RenderVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SceneId = table.Column<int>(type: "integer", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    UploadedById = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedById = table.Column<string>(type: "text", nullable: true),
                    AcceptedById = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RenderVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RenderVersions_AspNetUsers_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RenderVersions_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RevisionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RenderVersionId = table.Column<int>(type: "integer", nullable: false),
                    RequestedById = table.Column<string>(type: "text", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Classification = table.Column<int>(type: "integer", nullable: true),
                    ClassificationReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ClassifiedById = table.Column<string>(type: "text", nullable: true),
                    ClassifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RequirementsRevisionId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevisionRequests_AspNetUsers_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RevisionRequests_RenderVersions_RenderVersionId",
                        column: x => x.RenderVersionId,
                        principalTable: "RenderVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RevisionRequests_SceneRequirementsRevisions_RequirementsRev~",
                        column: x => x.RequirementsRevisionId,
                        principalTable: "SceneRequirementsRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_OrderId",
                table: "Attachments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_UploadedById",
                table: "Attachments",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_OrderActionRequests_OrderId_Kind",
                table: "OrderActionRequests",
                columns: new[] { "OrderId", "Kind" },
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RenderVersions_SceneId_VersionNumber",
                table: "RenderVersions",
                columns: new[] { "SceneId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RenderVersions_UploadedById",
                table: "RenderVersions",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionRequests_RenderVersionId",
                table: "RevisionRequests",
                column: "RenderVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionRequests_RequestedById",
                table: "RevisionRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionRequests_RequirementsRevisionId",
                table: "RevisionRequests",
                column: "RequirementsRevisionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "OrderActionRequests");

            migrationBuilder.DropTable(
                name: "RevisionRequests");

            migrationBuilder.DropTable(
                name: "RenderVersions");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "ClosedById",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "EndReason",
                table: "SceneAssignments");

            migrationBuilder.DropColumn(
                name: "EndedById",
                table: "SceneAssignments");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Orders");
        }
    }
}
