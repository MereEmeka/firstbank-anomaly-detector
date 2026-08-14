using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstBank.DataAccess.Migrations.AtmDB
{
    /// <inheritdoc />
    public partial class InitialAtmStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "atm");

            migrationBuilder.CreateTable(
                name: "Cards",
                schema: "atm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    PinHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "atm",
                table: "Cards",
                columns: new[] { "Id", "AccountId", "CardNumber", "FailedAttempts", "IsBlocked", "PinHash" },
                values: new object[] { new Guid("0c06deb6-7df7-4d7b-b205-edcdd0439486"), new Guid("55555555-5555-5555-5555-555555555555"), "1234567890123456", 0, false, "$2a$12$NqL1V/E.jO6Q0V7M1L8.peB1qF1.1.1.1.1.1.1.1.1.1.1.1.1.1.1" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cards",
                schema: "atm");
        }
    }
}
