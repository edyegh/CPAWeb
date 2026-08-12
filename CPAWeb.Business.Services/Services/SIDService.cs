using System;
using System.Collections.Generic;
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

        public SIDService(ISIDRepository sidRepository, IMapper mapper)
        {
            _sidRepository = sidRepository ?? throw new ArgumentNullException(nameof(sidRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<bool> AddSIDAsync(CreateSIDDto createDto)
        {
            var entity = _mapper.Map<SID>(createDto);
            return await _sidRepository.AddSIDAsync(entity);
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

                    // Տողերը կարդում ենք 1-ին կամ 2-րդ տողից (ըստ ձեր Excel structure-ի)
                    for (int row = 1; row <= rowCount; row++)
                    {
                        var cell = worksheet.Cells[row, 1]; // Առաջին սյունակ (Column A)

                        if (cell.Value != null && IsCellYellow(cell))
                        {
                            string val = cell.Value.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(val))
                            {
                                sheetDto.YellowRowFirstColumnValues.Add(val);
                            }
                        }
                    }

                    resultList.Add(sheetDto);
                }
            }

            return resultList;
        }

        // =========================================================================
        // 2. ԳՐԱՆՑԵԼ ՄԻԱՅՆ ԸՆՏՐՎԱԾ SHЕЕТ-Ի ՏՎՅԱԼՆԵՐԸ (UI-ի ԿՈՃԱԿԸ ՍԵՂՄԵԼԻՍ)
        // =========================================================================
        public async Task<int> SaveSheetDataAsync(ImportSheetRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.SheetName) || dto.Items == null || !dto.Items.Any())
            {
                return 0;
            }

            // SheetName-ից ("Nikita 5124" կամ "5124") կառուցում ենք ամբողջական համարը՝ 37488005124
            string? fullNumber = BuildFullNumber(dto.SheetName);

            // Եթե SheetName-ում թվեր չկան, փորձում ենք գտնել բազայից
            if (fullNumber == null)
            {
                fullNumber = await _sidRepository.GetFullNumberBySuffixAsync(dto.SheetName);
            }

            int successCount = 0;

            foreach (var sidName in dto.Items)
            {
                var newSid = new SID
                {
                    Name = sidName,
                    Number = fullNumber ?? dto.SheetName // Եթե չգտնի, դնում է SheetName-ը
                };

                bool isInserted = await _sidRepository.AddSIDAsync(newSid);
                if (isInserted)
                {
                    successCount++;
                }
            }

            return successCount;
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

        // Դեղին գույնը ստուգող մեթոդը
        private bool IsCellYellow(ExcelRange cell)
        {
            var fill = cell.Style.Fill;

            if (fill.PatternType == ExcelFillStyle.Solid)
            {
                string colorRgb = fill.BackgroundColor.Rgb;

                if (!string.IsNullOrEmpty(colorRgb))
                {
                    return colorRgb.EndsWith("FFFF00", StringComparison.OrdinalIgnoreCase) ||
                           colorRgb.EndsWith("FFFF99", StringComparison.OrdinalIgnoreCase);
                }

                if (fill.BackgroundColor.Indexed == 3 || fill.BackgroundColor.Indexed == 13)
                {
                    return true;
                }
            }

            return false;
        }
    }
}