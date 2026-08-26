using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CPAWeb.Services.DTOs;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CPAWeb.API.Auth
{
    public interface IJwtTokenGenerator
    {
        (string Token, DateTime ExpiresAtUtc) Generate(UserDto user);
    }

    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtOptions _options;

        public JwtTokenGenerator(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public (string Token, DateTime ExpiresAtUtc) Generate(UserDto user)
        {
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

            var claims = new List<Claim>
            {
                new Claim(CpaClaimTypes.UserId, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(CpaClaimTypes.Name, user.Name),
                new Claim(CpaClaimTypes.Email, user.Email),
                // Դերը՝ "users" կոճակի ցուցադրման և [Authorize(Roles = "Admin")]-ի համար
                new Claim(CpaClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAtUtc,
                signingCredentials: credentials);

            // OutboundClaimTypeMap-ը մաքրում ենք, որ claim-երի անունները չվերաձևակերպվեն
            var handler = new JwtSecurityTokenHandler();
            handler.OutboundClaimTypeMap.Clear();

            return (handler.WriteToken(token), expiresAtUtc);
        }
    }
}
