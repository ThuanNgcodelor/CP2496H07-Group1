using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CP2496H07Group1.Migrations
{
    /// <inheritdoc />
    public partial class Migrationaadsdasda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NewDebt",
                table: "CreditCards",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewDebt",
                table: "CreditCards");
        }
    }
}
