using FirstBank.Core.Models;
using FirstBank.DataAccess.Data;
using System;
using BCrypt.Net;
using System.Linq;
using System.Threading.Tasks;

namespace FirstBank.DataAccess.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(FirstDBContext context)
        {
            // The Fix: Check if your specific test batch is already in the database.
            // If Emeka is already there, we know the batch ran successfully.
            if (context.Users.Any(u => u.Email == "merechukwuemeka12docker-compose up -d@gmail.com"))
            {
                return;
            }

            // 1. Generate the Guids so we can link the Accounts to the Users
            var emekaUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var testUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var seyinUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");

            // 2. Create the Users
            var users = new[]
            {
                new AppUser
                {
                    UserId = emekaUserId,
                    FirstName = "Emeka",
                    LastName = "Mere",
                    Email = "merechukwuemeka12@gmail.com",
                    Role = "Customer",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Emeka2026!", 12)
                },
                new AppUser
                {
                    UserId = testUserId,
                    FirstName = "Test",
                    LastName = "Subject",
                    Email = "testemail@firstbank.com",
                    Role = "Customer",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12)
                },
                new AppUser
                {
                    UserId = seyinUserId,
                    FirstName = "Seyin",
                    LastName = "Alao",
                    Email = "seyinalao@gmail.com",
                    Role = "Customer",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Seyin2026!", 12)
                }
            };

            await context.Users.AddRangeAsync(users);

            // 3. Create the Linked Bank Accounts
            var accounts = new[]
            {
                new Account
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    UserId = emekaUserId,
                    AccountNumber = "1000000001",
                    Balance = 5000000m,
                    Currency = "NGN",
                    CreatedAt = DateTime.UtcNow
                },
                new Account
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    UserId = testUserId,
                    AccountNumber = "1000000002",
                    Balance = 15000.00m,
                    Currency = "NGN",
                    CreatedAt = DateTime.UtcNow
                },
                 new Account
                {
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    UserId = seyinUserId,
                    AccountNumber = "1000000003",
                    Balance = 25000.00m,
                    Currency = "NGN",
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Accounts.AddRangeAsync(accounts);

            // 4. Save everything to SQL Server
            await context.SaveChangesAsync();
        }
    }
}