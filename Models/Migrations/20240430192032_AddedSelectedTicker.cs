using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Models.Migrations
{
    /// <inheritdoc />
    public partial class AddedSelectedTicker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelectedTickers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SelectedTickers_Ticker",
                table: "SelectedTickers",
                column: "Ticker");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SelectedTickers");
        }
    }
}
