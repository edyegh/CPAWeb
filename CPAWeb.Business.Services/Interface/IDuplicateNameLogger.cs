using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CPAWeb.Services.DTOs;

namespace CPAWeb.Services.Interface
{
    // Կրկնվող անունները գրանցում է .txt ֆայլում, որպեսզի հետո երևան UI-ում
    public interface IDuplicateNameLogger
    {
        string FilePath { get; }

        Task AppendAsync(IEnumerable<string> names, string source);
        Task<List<DuplicateNameDto>> ReadAllAsync();
        Task ClearAsync();
    }
}
