using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CP2496H07Group1.Migrations
{
    /// <inheritdoc />
    public partial class Migrationa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InsuranceNumber",
                table: "UserInsurances",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InsuranceNumber",
                table: "UserInsurances");
        }
    }
}
