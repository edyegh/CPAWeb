using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CPAWeb.Data.Model;
using CPAWeb.Services.DTOs;
using Microsoft.AspNetCore.Http;

namespace CPAWeb.Services.Interface
{
    public interface ISIDService
    {
        // "add new name" — համարից service_id, service_id-ից account_id, ապա գրանցում
        // userName — մուտք գործած օգտատերը, գրվում է cpa_audit_trail.user_name սյունակում
        Task<AddNameResultDto> AddSIDAsync(CreateSIDDto createDto, string? userName);
        // Որոնում՝ ըստ SID-ի (SERVICE_LOCATOR_VALUE), provider-ի համարի կամ provider-ի անվան
        Task<List<SIDSearchResultDto>> SearchAsync(string value, SearchType type);

        // Նոր մեթոդները
        Task<List<ExcelSheetPreviewDto>> ParseExcelPreviewAsync(IFormFile file);

        // 1. Sheet-ի արժեքները դնում է ժամանակավոր աղյուսակում (edyeghiazaryan_insertvalue)
        Task<StageSheetResultDto> SaveSheetDataAsync(ImportSheetRequestDto dto);

        // 2. Ժամանակավոր աղյուսակի անունները գրանցում է նույն PL/SQL բլոկով, ինչ "add new name"-ը
        Task<AddNameResultDto> CommitStagedNamesAsync(CommitStagedRequestDto dto, string? userName);

        // 3. Կրկնվող (արդեն գրանցված) անունների ցանկը
        Task<List<DuplicateNameDto>> GetDuplicateNamesAsync();
        Task ClearDuplicateNamesAsync();
        string DuplicateNamesFilePath { get; }
    }
}
