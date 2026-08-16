using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InventorySystemCloud.Application.DTOs.Auth;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Application.Settings;
using InventorySystemCloud.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InventorySystemCloud.Infrastructure.Services
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _settings;

        public JwtTokenGenerator(IOptions<JwtSettings> settings) => _settings = settings.Value;

        public GeneratedToken GenerateToken(User user)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.PublicId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("security_stamp", user.SecurityStamp),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: credentials);

            return new GeneratedToken { Value = new JwtSecurityTokenHandler().WriteToken(token), ExpiresAt = expiresAt };
        }
    }
}
