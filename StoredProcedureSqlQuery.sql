USE FirstBankDb;
GO

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
    DECLARE @SourceBalance DECIMAL(18,2);
    
    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Check Source Existence & Balance
        SELECT @SourceBalance = Balance 
        FROM Accounts WITH (UPDLOCK)
        WHERE Id = @SourceAccountId;

        IF @SourceBalance IS NULL
            THROW 50001, 'Transaction Failed: Source account does not exist.', 1;

        IF @SourceBalance < @Amount
            THROW 50002, 'Transaction Failed: Insufficient funds in the source account.', 1;

        -- 2. Check Destination Existence
        IF NOT EXISTS (SELECT 1 FROM Accounts WITH (UPDLOCK) WHERE Id = @DestinationAccountId)
            THROW 50003, 'Transaction Failed: Destination account does not exist.', 1;

        -- 3. Update Balances
        UPDATE Accounts SET Balance = Balance - @Amount WHERE Id = @SourceAccountId;
        UPDATE Accounts SET Balance = Balance + @Amount WHERE Id = @DestinationAccountId;

        -- 4. Insert the Main Ledger Receipt
        INSERT INTO Transactions (Id, SourceAccountId, DestinationAccountId, Amount, Description, IsAnomaly, CreatedAt, Status)
        VALUES (@Id, @SourceAccountId, @DestinationAccountId, @Amount, @Description, @IsAnomaly, GETUTCDATE(), 'Completed');

        -- 5. Insert Idempotency Record
        INSERT INTO IdempotencyRecords ([Key], CreatedAt)
        VALUES (@IdempotencyKey, GETUTCDATE());

        -- 6. Insert Anomaly Log if flagged
        IF @IsAnomaly = 1
        BEGIN
            INSERT INTO AnomalyLogs (Id, TransactionId, FlagReason, LoggedAt)
            VALUES (NEWID(), @Id, 'Transaction amount exceeded the NGN 500,000 threshold.', GETUTCDATE());
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW; -- Sends the exact error message back to C#
    END CATCH
END;
GO

