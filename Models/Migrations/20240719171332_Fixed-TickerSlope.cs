using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Models.Migrations
{
    /// <inheritdoc />
    public partial class FixedTickerSlope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComputedSlope");

            migrationBuilder.AddColumn<string>(
                name: "SlopeResults",
                table: "TickerSlopes",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SlopeResults",
                table: "TickerSlopes");

            migrationBuilder.CreateTable(
                name: "ComputedSlope",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Intercept = table.Column<double>(type: "double precision", nullable: true),
                    Line = table.Column<decimal>(type: "numeric", nullable: true),
                    RSquared = table.Column<double>(type: "double precision", nullable: true),
                    Slope = table.Column<double>(type: "double precision", nullable: true),
                    StdDev = table.Column<double>(type: "double precision", nullable: true),
                    TickerSlopeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComputedSlope", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComputedSlope_TickerSlopes_TickerSlopeId",
                        column: x => x.TickerSlopeId,
                        principalTable: "TickerSlopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComputedSlope_TickerSlopeId",
                table: "ComputedSlope",
                column: "TickerSlopeId");
        }
    }
}
