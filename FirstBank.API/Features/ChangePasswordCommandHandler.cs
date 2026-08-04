using System.Threading;
using System.Threading.Tasks;
using FirstBank.Core.Models;
using FirstBank.DataAccess.Data;
using FirstBank.API.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FirstBank.API.Features
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ApiResponse<object>>
    {
        private readonly FirstDBContext _context;
        private readonly IEmailService _emailService;

        public ChangePasswordCommandHandler(FirstDBContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<ApiResponse<object>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);
            if (user == null)
            {
                return new ApiResponse<object> { Success = false, StatusCode = 404, Message = "User not found." };
            }

            // Verify old password (Assumes BCrypt usage)
            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            {
                return new ApiResponse<object> { Success = false, StatusCode = 400, Message = "Incorrect current password." };
            }

            // Hash new password and save
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            // Send Confirmation Email
            string emailBody = @"
                <h2>FirstBank Security Update</h2>
                <p>Your password was successfully changed.</p>
                <p>If you did not authorize this change, please contact our support team immediately.</p>";

            _ = _emailService.SendEmailAsync(user.Email, "FirstBank: Password Changed Successfully", emailBody);

            return new ApiResponse<object> { Success = true, StatusCode = 200, Message = "Password updated successfully." };
        }
    }
}