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
        Task<AddNameResultDto> AddSIDAsync(CreateSIDDto createDto);
        // Որոնում ըստ SERVICE_LOCATOR_VALUE-ի
        Task<List<SIDSearchResultDto>> SearchAsync(string value);

        // Նոր մեթոդները
        Task<List<ExcelSheetPreviewDto>> ParseExcelPreviewAsync(IFormFile file);

        // 1. Sheet-ի արժեքները դնում է ժամանակավոր աղյուսակում (edyeghiazaryan_insertvalue)
        Task<StageSheetResultDto> SaveSheetDataAsync(ImportSheetRequestDto dto);

        // 2. Կրկնվող (արդեն գրանցված) անունների ցանկը
        Task<List<DuplicateNameDto>> GetDuplicateNamesAsync();
        Task ClearDuplicateNamesAsync();
        string DuplicateNamesFilePath { get; }
    }
}
