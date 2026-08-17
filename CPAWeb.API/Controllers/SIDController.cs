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

        [HttpPost("parse-preview")]
        public async Task<IActionResult> ParsePreview(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("excel ֆայլը դատարկ է:");

            var result = await _sidService.ParseExcelPreviewAsync(file);
            return Ok(result);
        }

        // 2. Կոնկրետ 1 sheet-ի արժեքները ժամանակավոր աղյուսակ ուղարկելու endpoint
        [HttpPost("import-sheet")]
        public async Task<IActionResult> ImportSheet([FromBody] ImportSheetRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.SheetName))
                return BadRequest("անվավեր տվյալներ:");

            var result = await _sidService.SaveSheetDataAsync(dto);
            return Ok(result);
        }

        // 3. Ժամանակավոր աղյուսակի անունները գրանցել cpa_sid-ում տրված համարով և մաքրել աղյուսակը
        [HttpPost("commit-staged")]
        public async Task<IActionResult> CommitStaged([FromBody] CommitStagedRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Number))
                return BadRequest("համարը պարտադիր է:");

            int insertedCount = await _sidService.CommitStagedNamesAsync(dto.Number);

            if (insertedCount == 0)
                return BadRequest("ժամանակավոր աղյուսակում տվյալներ չկան:");

            return Ok(insertedCount);
        }

        // 4. Արդեն գրանցված (կրկնվող) անունների ցանկը
        [HttpGet("duplicates")]
        public async Task<IActionResult> GetDuplicates()
        {
            var result = await _sidService.GetDuplicateNamesAsync();
            return Ok(result);
        }

        // 5. Նույն ցանկը՝ .txt ֆայլով
        [HttpGet("duplicates/file")]
        public async Task<IActionResult> DownloadDuplicates()
        {
            string path = _sidService.DuplicateNamesFilePath;

            if (!System.IO.File.Exists(path))
                return NotFound("կրկնվող անուններ չկան:");

            var bytes = await System.IO.File.ReadAllBytesAsync(path);
            return File(bytes, "text/plain", "duplicate-names.txt");
        }

        // 6. Ցանկի մաքրում
        [HttpDelete("duplicates")]
        public async Task<IActionResult> ClearDuplicates()
        {
            await _sidService.ClearDuplicateNamesAsync();
            return NoContent();
        }
    }
}