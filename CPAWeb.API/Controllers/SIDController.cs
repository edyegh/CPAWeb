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

        [HttpPost("upload-excel")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Խնդրում ենք վերբեռնել ֆայլ:");
            }

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Խնդրում ենք վերբեռնել միայն .xlsx ֆորմատի Excel ֆայլ:");
            }

            int insertedCount = await _sidService.ProcessExcelFileAsync(file);

            return Ok(new
            {
                Message = $"Ֆայլը հաջողությամբ մշակվեց: Ավելացվեց {insertedCount} նոր տող:",
                InsertedCount = insertedCount
            });
        }
    }
}