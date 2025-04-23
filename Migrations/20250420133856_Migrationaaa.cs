using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CP2496H07Group1.Migrations
{
    /// <inheritdoc />
    public partial class Migrationaaa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserInsurances_Transactions_TransactionId",
                table: "UserInsurances");

            migrationBuilder.AddForeignKey(
                name: "FK_UserInsurances_Transactions_TransactionId",
                table: "UserInsurances",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserInsurances_Transactions_TransactionId",
                table: "UserInsurances");

            migrationBuilder.AddForeignKey(
                name: "FK_UserInsurances_Transactions_TransactionId",
                table: "UserInsurances",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
