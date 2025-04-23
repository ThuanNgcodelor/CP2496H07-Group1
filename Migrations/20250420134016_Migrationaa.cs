using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CP2496H07Group1.Migrations
{
    /// <inheritdoc />
    public partial class Migrationaa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserInsurances_InsurancePackages_PackageId",
                table: "UserInsurances");

            migrationBuilder.DropForeignKey(
                name: "FK_UserInsurances_Users_UserId",
                table: "UserInsurances");

            migrationBuilder.AddForeignKey(
                name: "FK_UserInsurances_InsurancePackages_PackageId",
                table: "UserInsurances",
                column: "PackageId",
                principalTable: "InsurancePackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserInsurances_Users_UserId",
                table: "UserInsurances",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserInsurances_InsurancePackages_PackageId",
                table: "UserInsurances");

            migrationBuilder.DropForeignKey(
                name: "FK_UserInsurances_Users_UserId",
                table: "UserInsurances");

            migrationBuilder.AddForeignKey(
                name: "FK_UserInsurances_InsurancePackages_PackageId",
                table: "UserInsurances",
                column: "PackageId",
                principalTable: "InsurancePackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserInsurances_Users_UserId",
                table: "UserInsurances",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
