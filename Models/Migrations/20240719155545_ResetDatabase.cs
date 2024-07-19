using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Models.Migrations
{
    /// <inheritdoc />
    public partial class ResetDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IndexComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ListedIndexes = table.Column<int>(type: "integer", nullable: false),
                    Sector = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubSector = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Ticker = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    SnPWeight = table.Column<float>(type: "real", nullable: false),
                    NasdaqWeight = table.Column<float>(type: "real", nullable: false),
                    DowWeight = table.Column<float>(type: "real", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexComponents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceByDate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ticker = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<double>(type: "double precision", nullable: false),
                    High = table.Column<double>(type: "double precision", nullable: false),
                    Low = table.Column<double>(type: "double precision", nullable: false),
                    Close = table.Column<double>(type: "double precision", nullable: false),
                    AdjClose = table.Column<double>(type: "double precision", nullable: false),
                    Volume = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceByDate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SelectedTickers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ticker = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Close = table.Column<double>(type: "double precision", nullable: false),
                    AnnualPercentGain = table.Column<double>(type: "double precision", nullable: false),
                    HalfYearlyPercentGain = table.Column<double>(type: "double precision", nullable: false),
                    QuarterYearlyPercentGain = table.Column<double>(type: "double precision", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelectedTickers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TickerSlopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    Ticker = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TickerSlopes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComputedSlope",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TickerSlopeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Slope = table.Column<double>(type: "double precision", nullable: true),
                    Intercept = table.Column<double>(type: "double precision", nullable: true),
                    StdDev = table.Column<double>(type: "double precision", nullable: true),
                    RSquared = table.Column<double>(type: "double precision", nullable: true),
                    Line = table.Column<decimal>(type: "numeric", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_IndexComponents_Ticker",
                table: "IndexComponents",
                column: "Ticker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceByDate_Ticker",
                table: "PriceByDate",
                column: "Ticker");

            migrationBuilder.CreateIndex(
                name: "IX_SelectedTickers_Ticker",
                table: "SelectedTickers",
                column: "Ticker");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComputedSlope");

            migrationBuilder.DropTable(
                name: "IndexComponents");

            migrationBuilder.DropTable(
                name: "PriceByDate");

            migrationBuilder.DropTable(
                name: "SelectedTickers");

            migrationBuilder.DropTable(
                name: "TickerSlopes");
        }
    }
}
