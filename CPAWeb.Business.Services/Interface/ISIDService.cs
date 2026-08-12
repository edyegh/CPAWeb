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
        Task<bool> AddSIDAsync(CreateSIDDto createDto);
        Task<SIDDto?> GetSIDByNameAsync(string name);

        // Նոր մեթոդները
        Task<List<ExcelSheetPreviewDto>> ParseExcelPreviewAsync(IFormFile file);

        // 1. Sheet-ի արժեքները դնում է ժամանակավոր աղյուսակում (edyeghiazaryan_insertvalue)
        Task<StageSheetResultDto> SaveSheetDataAsync(ImportSheetRequestDto dto);

        // 2. Ժամանակավոր աղյուսակի անունները գրանցում է cpa_sid-ում տրված համարով և մաքրում աղյուսակը
        Task<int> CommitStagedNamesAsync(string number);
    }
}
