using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CPAWeb.Data.Model;

namespace CPAWeb.Data.Interface
{
    public interface ISIDRepository
    {
        Task<SID?> GetSIDByNameAsync(string name);

        Task<string?> GetFullNumberBySuffixAsync(string suffix);

        // Ժամանակավոր աղյուսակ (edyeghiazaryan_insertvalue)
        Task<int> ReplaceStagingNamesAsync(IEnumerable<string> names);
        Task<int> GetStagingCountAsync();

        // Ստուգում է, թե ժամանակավոր աղյուսակի որ Name-երն արդեն գրանցված են cpa_sid-ում
        Task<List<string>> GetStagedNamesAlreadyInSIDAsync();

        Task<int> TransferStagingToSIDAsync(string number);
    }
}
