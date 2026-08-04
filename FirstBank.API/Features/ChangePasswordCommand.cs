using System;
using MediatR;
using FirstBank.Core.Models;

namespace FirstBank.API.Features
{
    public class ChangePasswordCommand : IRequest<ApiResponse<object>>
    {
        public Guid UserId { get; set; }
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}