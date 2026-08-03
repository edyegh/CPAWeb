using CPAWeb.Services.DTOs;
using CPAWeb.Services.Interface;
using CPAWeb.Services.DTOs;
using CPAWeb.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace CPAWeb.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SIDController : ControllerBase
    {
        private readonly ISIDService _sidService;

        public SIDController(ISIDService sidService)
        {
            _sidService = sidService;
        }

        [HttpGet("{name}")]
        public async Task<ActionResult<SIDDto>> GetByName(string name)
        {
            var result = await _sidService.GetSIDByNameAsync(name);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSIDDto createDto)
        {
            var isSuccess = await _sidService.AddSIDAsync(createDto);

            if (!isSuccess)
            {
                return BadRequest("Չհաջողվեց ավելացնել տվյալները:");
            }

            return CreatedAtAction(nameof(GetByName), new { name = createDto.Name }, createDto);
        }
    }
}