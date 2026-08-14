using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstBank.DataAccess.Migrations.AtmDB
{
    /// <inheritdoc />
    public partial class AddAtmStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "atm",
                table: "Cards",
                keyColumn: "Id",
                keyValue: new Guid("0c06deb6-7df7-4d7b-b205-edcdd0439486"));

            migrationBuilder.InsertData(
                schema: "atm",
                table: "Cards",
                columns: new[] { "Id", "AccountId", "CardNumber", "FailedAttempts", "IsBlocked", "PinHash" },
                values: new object[] { new Guid("66666666-6666-6666-6666-666666666666"), new Guid("55555555-5555-5555-5555-555555555555"), "1234567890123456", 0, false, "$2a$12$NqL1V/E.jO6Q0V7M1L8.peB1qF1.1.1.1.1.1.1.1.1.1.1.1.1.1.1" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "atm",
                table: "Cards",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"));

            migrationBuilder.InsertData(
                schema: "atm",
                table: "Cards",
                columns: new[] { "Id", "AccountId", "CardNumber", "FailedAttempts", "IsBlocked", "PinHash" },
                values: new object[] { new Guid("0c06deb6-7df7-4d7b-b205-edcdd0439486"), new Guid("55555555-5555-5555-5555-555555555555"), "1234567890123456", 0, false, "$2a$12$NqL1V/E.jO6Q0V7M1L8.peB1qF1.1.1.1.1.1.1.1.1.1.1.1.1.1.1" });
        }
    }
}
