using System;
using System.Threading;
using System.Threading.Tasks;
using FirstBank.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using FirstBank.DataAccess.Data;
using MediatR;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;

namespace FirstBank.API.Features
{
    public class AtmAuthCommandHandler : IRequestHandler<AtmAuthCommand, ApiResponse<object>>
    {
        private readonly AtmDBContext _atmContext; //Strictly using Atm Context
        private readonly IConfiguration _config;
        private readonly ILogger<AtmAuthCommandHandler> _logger;

        public AtmAuthCommandHandler(AtmDBContext atmContext, IConfiguration config, ILogger<AtmAuthCommandHandler> logger)
        {
            _atmContext = atmContext;
            _config = config;
            _logger = logger;
        }

        public async Task<ApiResponse<object>> Handle(AtmAuthCommand request, CancellationToken cancellationToken)
        {
            var card = await _atmContext.Cards.FirstOrDefaultAsync(c => c.CardNumber == request.CardNumber, cancellationToken);

            if (card == null) return new ApiResponse<object>
            {
                Success = false,
                StatusCode = 404,
                Message = "Card not recognized."
            };

            if (card.IsBlocked) return new ApiResponse<object>
            {
                Success = false,
                StatusCode = 403,
                Message = "CARD BLOCKED. Please contact your bank."
            };

            // Bulletproof PIN Verification (Handles BCrypt safely with plain-text fallback)
            bool isPinValid = false;
            try
            {
                if (!string.IsNullOrEmpty(card.PinHash) && card.PinHash.StartsWith("$2"))
                {
                    isPinValid = BCrypt.Net.BCrypt.Verify(request.Pin, card.PinHash);
                }
                else
                {
                    isPinValid = (card.PinHash == request.Pin);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "BCrypt verification failed. Falling back to plain-text check.");
                isPinValid = (card.PinHash == request.Pin);
            }

            if (!isPinValid)
            {
                card.FailedAttempts += 1;
                if (card.FailedAttempts >= 3) card.IsBlocked = true;

                await _atmContext.SaveChangesAsync(cancellationToken);

                return new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = 401,
                    Message = card.IsBlocked ? "CARD BLOCKED due to 3 failed PIN attempts." : $"Invalid PIN. {3 - card.FailedAttempts} attempts remaining."
                };
            }

            //SUCCESS - Reset Attempts
            card.FailedAttempts = 0;
            await _atmContext.SaveChangesAsync(cancellationToken);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            //The Atm token embeds the AccountId, acting as a bridge to the core banking Domain
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("CardId", card.Id.ToString()),
                new System.Security.Claims.Claim("AccountId", card.AccountId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"], audience: _config["Jwt:Audience"],
                claims: claims, expires: DateTime.UtcNow.AddMinutes(15), signingCredentials: credentials);

            return new ApiResponse<object>
            {
                Success = true,
                StatusCode = 200,
                Message = "[AUTHENTICATED]",
                Data = new { token = new JwtSecurityTokenHandler().WriteToken(token) }
            };
        }
    }
}