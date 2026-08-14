using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstBank.DataAccess.Migrations.AtmDB
{
    /// <inheritdoc />
    public partial class SyncAtmModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "atm",
                table: "Cards",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "PinHash",
                value: "$2a$11$qJVo2QJYfU7wCijVxWbQSur31Z.IK02bPMaxULU51m5JshRZKaqjq");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "atm",
                table: "Cards",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "PinHash",
                value: "$2a$12$NqL1V/E.jO6Q0V7M1L8.peB1qF1.1.1.1.1.1.1.1.1.1.1.1.1.1.1");
        }
    }
}
