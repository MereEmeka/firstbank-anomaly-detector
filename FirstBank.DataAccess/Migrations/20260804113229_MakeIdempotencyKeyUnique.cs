using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstBank.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MakeIdempotencyKeyUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_Key",
                table: "IdempotencyRecords",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdempotencyRecords_Key",
                table: "IdempotencyRecords");
        }
    }
}
