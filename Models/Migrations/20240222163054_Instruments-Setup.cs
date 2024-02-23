using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Models.Migrations
{
    /// <inheritdoc />
    public partial class InstrumentsSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IndexComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ListedIndexes = table.Column<int>(type: "int", nullable: false),
                    Sector = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SubSector = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Ticker = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    SnPWeight = table.Column<float>(type: "real", nullable: false),
                    NasdaqWeight = table.Column<float>(type: "real", nullable: false),
                    DowWeight = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexComponents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndexComponents_Ticker",
                table: "IndexComponents",
                column: "Ticker",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndexComponents");
        }
    }
}
