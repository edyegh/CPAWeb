using CPAWeb.API.Auth;
using CPAWeb.Data.Model;
using CPAWeb.Services.DTOs;
using CPAWeb.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPAWeb.API.Controllers
{
    // Ամբողջ controller-ը հասանելի է միայն ադմիններին — սա է "users" կոճակի հիմքը
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoles.Admin)]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserDto>>> GetAll()
        {
            var users = await _userService.GetUsersAsync();
            return Ok(users);
        }

        // Նոր օգտատեր՝ անուն, email, գաղտնաբառ
        [HttpPost]
        public async Task<ActionResult<CreateUserResultDto>> Create([FromBody] CreateUserDto dto)
        {
            var result = await _userService.CreateUserAsync(dto, CurrentUserEmail());

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // Ջնջում — ադմինը ինքն իրեն ջնջել չի կարող (տես UserService.DeleteUserAsync)
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<DeleteUserResultDto>> Delete(long id)
        {
            var result = await _userService.DeleteUserAsync(id, CurrentUserEmail());

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // Ո՞ր ադմինն է կանչում — գրվում է created_by սյունակում և ստուգվում ջնջելիս
        private string CurrentUserEmail()
        {
            return User.FindFirst(CpaClaimTypes.Email)?.Value
                ?? User.FindFirst(CpaClaimTypes.Name)?.Value
                ?? string.Empty;
        }
    }
}
