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
        Task<SIDDto> GetSIDByNameAsync(string name);
        Task<bool> AddSIDAsync(CreateSIDDto createDto);
        Task<int> ProcessExcelFileAsync(IFormFile file);
    }
}
