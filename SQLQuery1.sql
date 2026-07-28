INSERT into Accounts(ID, AccountNumber, Balance, Currency, CreatedAt)
VALUES(
	NEWID(),
	1000000007,
	700000.00,
	'NGN',
	GETUTCDATE());

SELECT * FROM Accounts
ORDER BY CreatedAt DESC;

SELECT * FROM Accounts;
WHERE AccountNumber = 1000000007;

---------------------------------------------------------

DECLARE @TestAccountId UNIQUEIDENTIFIER = '103EB058-836A-4447-9497-483F99F6B1A0';

-- 1. Check the hardcoded column in the Accounts table
SELECT Balance AS AccountsTableBalance 
FROM Accounts 
WHERE Id = @TestAccountId;

-- 2. Check the live calculated sum from the Transactions table
EXEC CalculateAccountBalance @p_account_id = @TestAccountId;

-- 3. View the raw transaction history to see what the API is adding up
SELECT Amount, SourceAccountId, DestinationAccountId, Status
FROM Transactions
WHERE SourceAccountId = @TestAccountId OR DestinationAccountId = @TestAccountId;

UPDATE Transactions
SET Status = 'Completed'
WHERE Status IS NULL OR Status = '';

-- This script calculates the real sum of all completed transactions and permanently updates the Accounts table to match.
UPDATE Accounts
SET Balance = (
    ISNULL((SELECT SUM(Amount) FROM Transactions WHERE DestinationAccountId = Accounts.Id AND Status = 'Completed'), 0) -
    ISNULL((SELECT SUM(Amount) FROM Transactions WHERE SourceAccountId = Accounts.Id AND Status = 'Completed'), 0)
);

--This query is to convert negative balances to positive balances in the Accounts table.
