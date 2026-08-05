using System;
using System.Threading.Tasks;
using AutoMapper;
using CPAWeb.Services.DTOs;
using CPAWeb.Data.Interface;
using CPAWeb.Services.Interface;
using CPAWeb.Data.Model;
using System.ComponentModel;
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

            if (entity == null)
            {
                return null;
            }

            return _mapper.Map<SIDDto>(entity);
        }

        public async Task<int> ProcessExcelFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Excel ֆայլը դատարկ է:");

            ExcelPackage.License.SetNonCommercialOrganization("CPAWeb");

            int successCount = 0;

            using (var stream = file.OpenReadStream())
            using (var package = new ExcelPackage(stream))
            {
                // Անցնում ենք Excel-ի բոլոր Sheet-երի վրայով հերթով
                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    string sheetName = worksheet.Name.Trim(); // Օրինակ՝ "9008"

                    // 1. Բազայից ուղղակի հարցումով գտնում ենք այս Sheet-ին համապատասխանող Number-ը
                    string? fullNumber = await _sidRepository.GetFullNumberBySuffixAsync(sheetName);

                    // Եթե բազայում այս վերջավորությամբ Number չկա, անցնում ենք հաջորդ Sheet-ին
                    if (string.IsNullOrEmpty(fullNumber))
                    {
                        continue;
                    }

                    int rowCount = worksheet.Dimension?.Rows ?? 0;

                    // 2. Կարդում ենք Sheet-ի տողերը հերթով (սկսած 2-րդ տողից)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var cell = worksheet.Cells[row, 1]; // Առաջին սյունը (Column A)

                        if (cell.Value == null) continue;

                        string cellValue = cell.Value.ToString()?.Trim() ?? "";

                        // Ստուգում ենք՝ արդյոք բջիջը դեղին է
                        if (IsCellYellow(cell))
                        {
                            var newSid = new SID
                            {
                                Name = cellValue,
                                Number = fullNumber
                            };

                            // 3. ՀԵՐԹՈՎ ԻՆՍԵՌՏ․ Սպասում ենք մինչև նախորդը հաջողությամբ ավելանա բազայում
                            bool isInserted = await _sidRepository.AddSIDAsync(newSid);  //poxelu enq vor ayl tablicayum insert ani 

                            if (isInserted)
                            {
                                successCount++;
                            }
                            else
                            {
                                // Եթե տվյալ տողը չավելացավ, կարող եք լոգավորել կամ որոշում կայացնել՝ կանգնեցնե՞լ, թե շարունակել
                            }
                        }
                    }
                }
            }

            return successCount;
        }

        // Դեղին գույնը ստուգող օժանդակ մեթոդը
        private bool IsCellYellow(ExcelRange cell)
        {
            var fill = cell.Style.Fill;

            // 1. Ստուգում ենք, որ բջիջն ունի Solid լցոնում (PatternType)
            if (fill.PatternType == ExcelFillStyle.Solid)
            {
                string colorRgb = fill.BackgroundColor.Rgb;

                // 2. Եթե HEX/RGB կոդը առկա է, ստուգում ենք դեղինի ֆորմատները (FFFF00 կամ ARGB՝ FFFFFF00)
                if (!string.IsNullOrEmpty(colorRgb))
                {
                    return colorRgb.EndsWith("FFFF00", StringComparison.OrdinalIgnoreCase);
                }

                // 3. Եթե օգտագործված է Indexed Color (օրինակ՝ Excel-ի стандарт դեղինը)
                if (fill.BackgroundColor.Indexed == 3 || fill.BackgroundColor.Indexed == 13)
                {
                    return true;
                }
            }

            return false;
        }
    }
}