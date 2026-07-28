using System;
using FluentValidation;
using FirstBank.API.DTOs;

namespace FirstBank.API.Validators
{
    public class CreateTransactionValidator : AbstractValidator<CreateTransactionRequest>
    {
        public CreateTransactionValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero.")
                .LessThanOrEqualTo(5000000).WithMessage("Amount exceeds the NGN5,000,000 limit.");

            RuleFor(x => x.SourceAccountId)
                .NotEmpty()
                .Must(BeAValidGuid).WithMessage("Source Account ID must be a valid GUID.");

            RuleFor(x => x.DestinationAccountId)
                .NotEmpty()
                .Must(BeAValidGuid).WithMessage("Destination Account ID must be a valid GUID.");

            RuleFor(x => x.Description)
                .NotEmpty()
                .MaximumLength(255);
        }

        //Helper Method to ensure the string can actually be parsed into a real Database GUID
        private bool BeAValidGuid(string id)
        {
            return Guid.TryParse(id, out _);
        }
    }
}