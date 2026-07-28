UPDATE Accounts
SET Balance = Balance + 50000
WHERE AccountNumber = 1000000003;

SELECT * FROM Accounts;



INSERT INTO Accounts (Id, AccountNumber, Balance, Currency, CreatedAt)
VALUES 
-- Recreating the Source Account
('B065375E-3C3D-4416-934F-BB969D31B17A', '1000000007', 1000000.00, 'NGN', GETUTCDATE()),

-- Recreating the Destination Account
('103EB058-836A-4447-9497-483F99F6B1A0', '1000000001', 500000.00, 'NGN', GETUTCDATE());



SELECT * FROM Transactions 
WHERE SourceAccountId = '43F08DC8-A422-408F-B52D-765F89B5C4DA' 
   OR DestinationAccountId = '43F08DC8-A422-408F-B52D-765F89B5C4DA';

   UPDATE Transactions 
SET Status = 'Completed' 
WHERE Status IS NULL OR Status = '';

GO

--This code is to change all negative balances to positive balances in the Accounts table.
CREATE PROCEDURE ConvertNegativeBalances
AS
BEGIN
    -- We only target rows where the balance is below zero to save processing power.
    -- ABS() mathematically converts any negative number to its positive equivalent.
    UPDATE Accounts
    SET Balance = ABS(Balance)
    WHERE Balance < 0;
END;
GO

EXEC ConvertNegativeBalances;