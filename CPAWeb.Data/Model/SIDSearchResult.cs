using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPAWeb.Data.Model
{
    // Որոնման արդյունքը՝ CPA_NUMBER + CPA_SERVICE_IDENT + CPA_PROVIDER
    public class SIDSearchResult
    {
        // cp.NAME — provider-ի անունը
        public string ProviderName { get; set; } = string.Empty;

        // cn.SERVICE_NAME
        public string ServiceName { get; set; } = string.Empty;

        // cn.UP
        public string Up { get; set; } = string.Empty;

        // cs.SERVICE_LOCATOR_VALUE
        public string ServiceLocatorValue { get; set; } = string.Empty;

        // cs.SERVICE_ID
        public string ServiceId { get; set; } = string.Empty;
    }
}
