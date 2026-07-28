using FirstBank.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Principal;

namespace FirstBank.DataAccess.Data
{
    public class FirstDBContext : DbContext
    {
        public FirstDBContext(DbContextOptions<FirstDBContext> options) : base(options) { }
        
        //This tells EF Core to create a table called "Users" based on the AppUser class
        public DbSet<AppUser> Users { get; set; }

        //This tells EF Core to create a table called "Accounts" based on the Account class
        public DbSet<Account> Accounts { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<AnomalyLog> AnomalyLogs { get; set; }
        public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }

        //OnModelCreating method usinf Fluent API to enforce money strictness
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //This calls the base method first to ensure EF Core sets up properly
            base.OnModelCreating(modelBuilder);

            //This creates a static GUID for the Admin so EF Core does not recreate it on every migration
            var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111"); //Follws the 8-4-4-4-12 Format

            modelBuilder.Entity<AppUser>().HasData(
                new AppUser
                {
                    UserId = adminId,
                    Email = "admin@firstbank.com",
                    PasswordHash = "$2a$11$qJVo2QJYfU7wCijVxWbQSur31Z.IK02bPMaxULU51m5JshRZKaqjq",
                    Role = "Admin"
                }
            );

            //Tells SQL Server to use decimal(18,2) for the Amount property in the Transaction class
            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);
        }

    }
}