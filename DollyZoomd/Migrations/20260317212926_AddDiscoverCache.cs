using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DollyZoomd.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscoverCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                table: "shows",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PremieredOn",
                table: "shows",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiscoverCaches",
                columns: table => new
                {
                    CategoryName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RankPosition = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ShowId = table.Column<int>(type: "integer", nullable: false),
                    CachedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoverCaches", x => new { x.CategoryName, x.RankPosition });
                    table.ForeignKey(
                        name: "FK_DiscoverCaches_shows_ShowId",
                        column: x => x.ShowId,
                        principalTable: "shows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscoverCaches_CategoryName_ExpiryAtUtc",
                table: "DiscoverCaches",
                columns: new[] { "CategoryName", "ExpiryAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscoverCaches_ExpiryAtUtc",
                table: "DiscoverCaches",
                column: "ExpiryAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoverCaches_ShowId",
                table: "DiscoverCaches",
                column: "ShowId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscoverCaches");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "shows");

            migrationBuilder.DropColumn(
                name: "PremieredOn",
                table: "shows");
        }
    }
}
