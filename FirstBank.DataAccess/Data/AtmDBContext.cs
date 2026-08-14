using FirstBank.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace FirstBank.DataAccess.Data
{
    public class AtmDBContext : DbContext
    {
        public AtmDBContext(DbContextOptions<AtmDBContext> options) : base(options) { }

        public DbSet<Card> Cards { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //This enforces the schema boundary
            modelBuilder.HasDefaultSchema("atm");

            //Seeding a test card for Emeka's AccountID
            var emekaAccountId = Guid.Parse("55555555-5555-5555-5555-555555555555");

            modelBuilder.Entity<Card>().HasData(
                new Card
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    AccountId = emekaAccountId,
                    CardNumber = "1234567890123456",
                    PinHash = "$2a$11$qJVo2QJYfU7wCijVxWbQSur31Z.IK02bPMaxULU51m5JshRZKaqjq",
                    FailedAttempts = 0,
                    IsBlocked = false
                });
        }

    }
}
