using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CP2496H07Group1.Migrations
{
    /// <inheritdoc />
    public partial class Migrationaadsdaasascx : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Cvv",
                table: "CreditCards",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpirationDate",
                table: "CreditCards",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cvv",
                table: "CreditCards");

            migrationBuilder.DropColumn(
                name: "ExpirationDate",
                table: "CreditCards");
        }
    }
}
