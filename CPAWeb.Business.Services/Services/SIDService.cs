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

        // cpa_audit_trail-ի user_name սյունակի արժեքը
        public const string AuditUserName = "EdYeghiazaryan";

        // cpa_service_ident.traffic_type_id
        public const int TrafficTypeId = 1;

        // =========================================================================
        // "add new name" կոճակը
        //   1) Number -> CPA_NUMBER (status = 1) -> SERVICE_ID + SERVICE_NAME
        //   2) SERVICE_ID -> CPA_ACCOUNT_SERVICE_IDENT -> ACCOUNT_ID
        //   3) Name -> edyeghiazaryan_insertvalue.locator_value
        //   4) PL/SQL բլոկ՝ cpa_service_ident + cpa_account_service_ident + cpa_audit_trail
        //   5) ժամանակավոր աղյուսակի մաքրում
        // =========================================================================
        public async Task<AddNameResultDto> AddSIDAsync(CreateSIDDto createDto)
        {
            var result = new AddNameResultDto();

            if (createDto == null || string.IsNullOrWhiteSpace(createDto.Name))
            {
                throw new ArgumentException("Name cannot be empty.", nameof(createDto));
            }

            if (string.IsNullOrWhiteSpace(createDto.Number))
            {
                throw new ArgumentException("Number cannot be empty.", nameof(createDto));
            }

            string name = createDto.Name.Trim();
            string number = new string(createDto.Number.Where(char.IsDigit).ToArray());

            if (number.Length == 0)
            {
                throw new ArgumentException("Number must contain digits.", nameof(createDto));
            }

            // --- 1. Համարից՝ service_id ---
            var services = await _sidRepository.FindServicesByNumberAsync(number);

            if (services.Count == 0)
            {
                result.Message = $"no active service found for number '{number}'.";
                return result;
            }

            var distinctServices = services.Select(s => s.ServiceId).Distinct().ToList();
            if (distinctServices.Count > 1)
            {
                result.Message = $"number '{number}' matches several services ({string.Join(", ", distinctServices)}). " +
                                  "please enter a more specific number.";
                return result;
            }

            var service = services[0];

            result.ServiceId = service.ServiceId;
            result.ServiceName = service.ServiceName;
            result.ProviderName = service.ProviderName;

            // --- 2. service_id-ից՝ account_id ---
            var accountIds = await _sidRepository.GetAccountIdsForServiceAsync(service.ServiceId);

            if (accountIds.Count == 0)
            {
                result.Message = $"no account found for service_id {service.ServiceId}.";
                return result;
            }

            // Ամենաշատ գործածվող account-ը; մնացածները ցույց ենք տալիս UI-ում
            result.AccountId = accountIds[0];
            result.AccountCandidates = accountIds;

            // --- 3. Անունը ժամանակավոր աղյուսակում ---
            await _sidRepository.ReplaceStagingNamesAsync(new[] { name });

            // Պարտադիր ստուգում՝ արդյոք այս անունն արդեն գրանցված է
            result.AlreadyRegistered = await CheckStagedDuplicatesAsync("add new name");

            // --- 4. Գրանցում ---
            try
            {
                result.RegisteredCount = await _sidRepository.RegisterStagedNamesAsync(
                    service.ServiceId,
                    result.AccountId,
                    service.ServiceName,
                    AuditUserName,
                    TrafficTypeId);
            }
            finally
            {
                // --- 5. Ժամանակավոր աղյուսակը միշտ մաքրում ենք ---
                await _sidRepository.ClearStagingAsync();
            }

            result.Success = result.RegisteredCount > 0;
            result.Message = result.Success
                ? $"'{name}' registered for service_id {service.ServiceId} / account_id {result.AccountId}."
                : $"'{name}' is already registered in cpa_service_ident — nothing was inserted.";

            return result;
        }

        public async Task<List<SIDSearchResultDto>> SearchAsync(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Search value cannot be empty.", nameof(value));
            }

            var entities = await _sidRepository.SearchByServiceLocatorAsync(value.Trim());
            return _mapper.Map<List<SIDSearchResultDto>>(entities);
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

            // Պարտադիր ստուգում՝ որ անուններն արդեն գրանցված են
            result.DuplicateNames = await CheckStagedDuplicatesAsync(dto.SheetName);

            return result;
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