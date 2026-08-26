using CPAWeb.API.Auth;
using CPAWeb.Services.DTOs;
using CPAWeb.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPAWeb.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IJwtTokenGenerator _tokenGenerator;

        public AuthController(IUserService userService, IJwtTokenGenerator tokenGenerator)
        {
            _userService = userService;
            _tokenGenerator = tokenGenerator;
        }

        // Մուտք — հաջողվում է միայն այն դեպքում, երբ ադմինը այս օգտատիրոջը արդեն ավելացրել է
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginRequestDto request)
        {
            var result = await _userService.AuthenticateAsync(request);

            if (!result.Success || result.User == null)
                return Unauthorized(result);

            var (token, expiresAtUtc) = _tokenGenerator.Generate(result.User);

            result.Token = token;
            result.ExpiresAtUtc = expiresAtUtc;

            return Ok(result);
        }

        // Ընթացիկ օգտատերը՝ token-ի վավերականությունը ստուգելու համար
        [Authorize]
        [HttpGet("me")]
        public ActionResult<UserDto> Me()
        {
            return Ok(new UserDto
            {
                Id = long.TryParse(User.FindFirst(CpaClaimTypes.UserId)?.Value, out var id) ? id : 0,
                Name = User.FindFirst(CpaClaimTypes.Name)?.Value ?? string.Empty,
                Email = User.FindFirst(CpaClaimTypes.Email)?.Value ?? string.Empty,
                Role = User.FindFirst(CpaClaimTypes.Role)?.Value ?? "User",
                IsActive = true
            });
        }
    }
}
