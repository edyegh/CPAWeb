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
        // Որոնում ըստ SERVICE_LOCATOR_VALUE-ի (CPA_NUMBER + CPA_SERVICE_IDENT + CPA_PROVIDER)
        Task<List<SIDSearchResult>> SearchByServiceLocatorAsync(string value);

        // Ժամանակավոր աղյուսակ (edyeghiazaryan_insertvalue)
        Task<int> ReplaceStagingNamesAsync(IEnumerable<string> names);
        Task<int> GetStagingCountAsync();

        // Ստուգում է, թե ժամանակավոր աղյուսակի որ Name-երն արդեն գրանցված են և որտեղ
        Task<List<RegisteredNameInfo>> GetStagedNamesAlreadyRegisteredAsync();

        Task ClearStagingAsync();

        // "add new name" — համարով գտնում ենք SERVICE_ID-ն (CPA_NUMBER, status = 1)
        Task<List<ServiceLookup>> FindServicesByNumberAsync(string number);

        // SERVICE_ID-ով գտնում ենք ACCOUNT_ID-ն (CPA_ACCOUNT_SERVICE_IDENT)
        Task<List<long>> GetAccountIdsForServiceAsync(long serviceId);

        // Ժամանակավոր աղյուսակի անունները գրանցում ենք cpa_service_ident /
        // cpa_account_service_ident / cpa_audit_trail աղյուսակներում
        Task<int> RegisterStagedNamesAsync(long serviceId, long accountId, string serviceName, string userName, int trafficTypeId);
    }
}
