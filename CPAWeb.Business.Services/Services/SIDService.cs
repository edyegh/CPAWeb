using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CPAWeb.Services.DTOs;
using CPAWeb.Data.Interface;
using CPAWeb.Services.Interface;
using CPAWeb.Data.Model;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Microsoft.AspNetCore.Http;

namespace CPAWeb.Business.Services.Services
{
    public class SIDService : ISIDService
    {
        private readonly ISIDRepository _sidRepository;
        private readonly IMapper _mapper;
        private readonly IDuplicateNameLogger _duplicateLogger;

        public SIDService(ISIDRepository sidRepository, IMapper mapper, IDuplicateNameLogger duplicateLogger)
        {

            _sidRepository = sidRepository ?? throw new ArgumentNullException(nameof(sidRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _duplicateLogger = duplicateLogger ?? throw new ArgumentNullException(nameof(duplicateLogger));
        }

        public string DuplicateNamesFilePath => _duplicateLogger.FilePath;

        public Task<List<DuplicateNameDto>> GetDuplicateNamesAsync() => _duplicateLogger.ReadAllAsync();

        public Task ClearDuplicateNamesAsync() => _duplicateLogger.ClearAsync();

        // Ժամանակավոր աղյուսակ ավելացնելուց հետո պարտադիր SELECT ստուգում.
        // արդեն գրանցված անունները գրանցում ենք .txt ֆայլում և վերադարձնում UI-ին
        private async Task<List<string>> CheckStagedDuplicatesAsync(string source)
        {
            var duplicates = await _sidRepository.GetStagedNamesAlreadyInSIDAsync();

            if (duplicates.Count > 0)
            {
                await _duplicateLogger.AppendAsync(duplicates, source);
            }

            return duplicates;
        }

        // "add new name" կոճակը նույնպես անցնում է ժամանակավոր աղյուսակով.
        // 1) Name -> edyeghiazaryan_insertvalue, 2) -> cpa_sid տրված Number-ով, 3) TRUNCATE
        public async Task<bool> AddSIDAsync(CreateSIDDto createDto)
        {
            var entity = _mapper.Map<SID>(createDto);

            if (entity == null || string.IsNullOrWhiteSpace(entity.Name))
            {
                throw new ArgumentException("Name cannot be empty.", nameof(createDto));
            }

            await _sidRepository.ReplaceStagingNamesAsync(new[] { entity.Name });

            // Պարտադիր ստուգում՝ արդյոք այս անունն արդեն գրանցված է
            await CheckStagedDuplicatesAsync("add new name");

            int inserted = await CommitStagedNamesAsync(entity.Number);
            return inserted > 0;
        }

        public async Task<SIDDto?> GetSIDByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be empty.", nameof(name));
            }

            var entity = await _sidRepository.GetSIDByNameAsync(name);
            return entity == null ? null : _mapper.Map<SIDDto>(entity);
        }

        // =========================================================================
        // 1. ԿԱՐԴԱԼ EXCEL-Ը ԵՎ ՎԵՐԱԴԱՐՁՆԵԼ PREVIEW (ԱՌԱՆՑ ԲԱԶԱՅՈՒՄ ՊԱՀՊԱՆԵԼՈՒ)
        // =========================================================================
        public async Task<List<ExcelSheetPreviewDto>> ParseExcelPreviewAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Excel ֆայլը դատարկ է:");

            ExcelPackage.License.SetNonCommercialOrganization("CPAWeb");

            var resultList = new List<ExcelSheetPreviewDto>();

            using (var stream = file.OpenReadStream())
            using (var package = new ExcelPackage(stream))
            {
                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    var sheetDto = new ExcelSheetPreviewDto
                    {
                        SheetName = worksheet.Name.Trim()
                    };

                    int rowCount = worksheet.Dimension?.Rows ?? 0;

                    // Կարդում ենք ներքևից վերև՝ մինչև առաջին տողը, որը նարնջագույն չէ
                    int row = rowCount;

                    // Բաց ենք թողնում վերջի դատարկ ու չգունավորված տողերը
                    while (row >= 1)
                    {
                        var trailing = worksheet.Cells[row, 1]; // Առաջին սյունակ (Column A)

                        if (trailing.Value != null || IsCellOrange(trailing))
                            break;

                        row--;
                    }

                    for (; row >= 1; row--)
                    {
                        var cell = worksheet.Cells[row, 1];

                        // Առաջին ոչ նարնջագույն տողի վրա կանգ ենք առնում
                        if (!IsCellOrange(cell))
                            break;

                        string val = cell.Value?.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(val))
                        {
                            sheetDto.MarkedRowFirstColumnValues.Add(val);
                        }
                    }

                    // Վերադարձնում ենք Excel-ի բնական հերթականությամբ (վերևից ներքև)
                    sheetDto.MarkedRowFirstColumnValues.Reverse();

                    resultList.Add(sheetDto);
                }
            }

            return resultList;
        }

        // =========================================================================
        // 2. ԳՐԱՆՑԵԼ ՄԻԱՅՆ ԸՆՏՐՎԱԾ SHЕЕТ-Ի ՏՎՅԱԼՆԵՐԸ (UI-ի ԿՈՃԱԿԸ ՍԵՂՄԵԼԻՍ)
        // =========================================================================
        public async Task<StageSheetResultDto> SaveSheetDataAsync(ImportSheetRequestDto dto)
        {
            var result = new StageSheetResultDto
            {
                SheetName = dto?.SheetName ?? string.Empty
            };

            if (dto == null || string.IsNullOrWhiteSpace(dto.SheetName) || dto.Items == null || !dto.Items.Any())
            {
                return result;
            }

            // Ընտրված sheet-ի արժեքները դնում ենք ժամանակավոր աղյուսակի Name սյունակում
            result.StagedCount = await _sidRepository.ReplaceStagingNamesAsync(dto.Items);

            // Պարտադիր ստուգում՝ որ անուններն արդեն գրանցված են cpa_sid-ում
            result.DuplicateNames = await CheckStagedDuplicatesAsync(dto.SheetName);

            // SheetName-ից ("Nikita 5124" կամ "5124") առաջարկում ենք համարը՝ 37488005124
            result.SuggestedNumber = BuildFullNumber(dto.SheetName)
                                     ?? await _sidRepository.GetFullNumberBySuffixAsync(dto.SheetName);

            return result;
        }

        // =========================================================================
        // 3. ԺԱՄԱՆԱԿԱՎՈՐ ԱՂՅՈՒՍԱԿԻՑ cpa_sid ԵՎ ՄԱՔՐՈՒՄ (TRUNCATE)
        // =========================================================================
        public async Task<int> CommitStagedNamesAsync(string number)
        {
            if (string.IsNullOrWhiteSpace(number))
            {
                throw new ArgumentException("Համարը դատարկ է:", nameof(number));
            }

            string cleanNumber = new string(number.Where(char.IsDigit).ToArray());

            if (cleanNumber.Length == 0)
            {
                throw new ArgumentException("Համարը պետք է պարունակի թվեր:", nameof(number));
            }

            return await _sidRepository.TransferStagingToSIDAsync(cleanNumber);
        }

        // =========================================================================
        // SHEET NAME -> ԱՄԲՈՂՋԱԿԱՆ ՀԱՄԱՐ
        // "Nikita 5124" -> 37488005124 ; "5124" -> 37488005124
        // =========================================================================
        public const string NumberPrefix = "3748800";
        public const int NumberLength = 11;

        public static string? BuildFullNumber(string? sheetName)
        {
            if (string.IsNullOrWhiteSpace(sheetName))
                return null;

            // Վերցնում ենք անվան մեջ եղած վերջին թվային խումբը ("Nikita 5124" -> "5124")
            var matches = System.Text.RegularExpressions.Regex.Matches(sheetName, @"\d+");
            if (matches.Count == 0)
                return null;

            string digits = matches[matches.Count - 1].Value;

            // Եթե արդեն ամբողջական համար է, թողնում ենք ինչպես կա
            if (digits.Length >= NumberLength)
                return digits.Substring(digits.Length - NumberLength);

            int suffixLength = NumberLength - NumberPrefix.Length; // 4
            string suffix = digits.Length > suffixLength
                ? digits.Substring(digits.Length - suffixLength)
                : digits.PadLeft(suffixLength, '0');

            return NumberPrefix + suffix;
        }

        // Նարնջագույնը ստուգող մեթոդը
        private bool IsCellOrange(ExcelRange cell)
        {
            var fill = cell.Style.Fill;

            if (fill.PatternType != ExcelFillStyle.Solid)
                return false;

            string? argb = GetFillArgb(fill.BackgroundColor);

            if (string.IsNullOrEmpty(argb) || argb.Length < 6)
                return false;

            // Վերջին 6 նիշը՝ RRGGBB
            string rgb = argb.Substring(argb.Length - 6);

            if (!int.TryParse(rgb.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r) ||
                !int.TryParse(rgb.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g) ||
                !int.TryParse(rgb.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b))
            {
                return false;
            }

            return IsOrange(r, g, b);
        }

        // Theme/indexed գույների դեպքում Rgb-ն դատարկ է, ուստի փորձում ենք LookupColor()
        private static string? GetFillArgb(ExcelColor color)
        {
            if (!string.IsNullOrEmpty(color.Rgb))
                return color.Rgb;

            try
            {
                return color.LookupColor()?.TrimStart('#');
            }
            catch
            {
                return null;
            }
        }

        // Նարնջագույն = hue 10..50 աստիճան, բավարար հագեցվածությամբ
        // (դեղինը՝ 60, կարմիրը՝ 0, դուրս են մնում)
        private static bool IsOrange(int r, int g, int b)
        {
            int max = Math.Max(r, Math.Max(g, b));
            int min = Math.Min(r, Math.Min(g, b));
            int delta = max - min;

            if (delta == 0 || max < 100)
                return false;

            double saturation = (double)delta / max;
            if (saturation < 0.20)
                return false;

            // Նարնջագույնի դեպքում կարմիրը միշտ ամենամեծն է
            if (max != r)
                return false;

            double hue = 60.0 * (((double)(g - b) / delta) % 6.0);
            if (hue < 0)
                hue += 360.0;

            return hue >= 10.0 && hue <= 50.0;
        }
    }
}