using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstBank.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnomalyLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlagReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnomalyLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsAnomalyFlagged = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "Email", "PasswordHash", "Role" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "admin@firstbank.com", "$2a$11$qJVo2QJYfU7wCijVxWbQSur31Z.IK02bPMaxULU51m5JshRZKaqjq", "Admin" });

            migrationBuilder.Sql(@"
                CREATE PROCEDURE ExecuteTransfer
                    @Id UNIQUEIDENTIFIER,
                    @SourceAccountId UNIQUEIDENTIFIER,
                    @DestinationAccountId UNIQUEIDENTIFIER,
                    @Amount DECIMAL(18,2),
                    @Description NVARCHAR(255),
                    @IsAnomaly BIT,
                    @IdempotencyKey NVARCHAR(255)
                AS
                BEGIN
                    -- 1. IDEMPOTENCY CHECK: If we've seen this key, exit cleanly without doing anything
                    IF EXISTS (SELECT 1 FROM IdempotencyRecords WHERE [Key] = @IdempotencyKey)
                    BEGIN
                        RETURN;
                    END

                    -- 2. VALIDATE AMOUNT & ACCOUNTS
                    IF @Amount <= 0
                        THROW 50004, 'Transaction Failed: Transfer amount must be greater than zero.', 1;

                    IF @SourceAccountId = @DestinationAccountId
                        THROW 50005, 'Transaction Failed: Source and destination accounts cannot be the same.', 1;

                    DECLARE @SourceBalance DECIMAL(18,2);
                    
                    BEGIN TRY
                        BEGIN TRANSACTION;

                        -- 3. Check Source Existence & Balance
                        SELECT @SourceBalance = Balance 
                        FROM Accounts WITH (UPDLOCK)
                        WHERE Id = @SourceAccountId;

                        IF @SourceBalance IS NULL
                            THROW 50001, 'Transaction Failed: Source account does not exist.', 1;

                        IF @SourceBalance < @Amount
                            THROW 50002, 'Transaction Failed: Insufficient funds in the source account.', 1;

                        -- 4. Check Destination Existence
                        IF NOT EXISTS (SELECT 1 FROM Accounts WITH (UPDLOCK) WHERE Id = @DestinationAccountId)
                            THROW 50003, 'Transaction Failed: Destination account does not exist.', 1;

                        -- 5. Update Balances
                        UPDATE Accounts SET Balance = Balance - @Amount WHERE Id = @SourceAccountId;
                        UPDATE Accounts SET Balance = Balance + @Amount WHERE Id = @DestinationAccountId;

                        -- 6. Insert the Main Ledger Receipt
                        INSERT INTO Transactions (Id, SourceAccountId, DestinationAccountId, Amount, Description, IsAnomalyFlagged, CreatedAt, Status)
                        VALUES (@Id, @SourceAccountId, @DestinationAccountId, @Amount, @Description, @IsAnomaly, GETUTCDATE(), 'Completed');

                        -- 7. Insert Idempotency Record
                        INSERT INTO IdempotencyRecords ([Key], CreatedAt)
                        VALUES (@IdempotencyKey, GETUTCDATE());

                        -- 8. Insert Anomaly Log if flagged
                        IF @IsAnomaly = 1
                        BEGIN
                            INSERT INTO AnomalyLogs (Id, TransactionId, FlagReason, LoggedAt)
                            VALUES (NEWID(), @Id, 'Transaction amount exceeded the NGN 500,000 threshold.', GETUTCDATE());
                        END

                        COMMIT TRANSACTION;
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                        THROW; 
                    END CATCH
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "AnomalyLogs");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS ExecuteTransfer");
        }
    }
}
