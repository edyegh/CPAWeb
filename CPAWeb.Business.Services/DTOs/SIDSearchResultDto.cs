using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPAWeb.Services.DTOs
{
    // Որոնման արդյունքը՝ CPA_NUMBER + CPA_SERVICE_IDENT + CPA_PROVIDER
    public class SIDSearchResultDto
    {
        public string ProviderName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Up { get; set; } = string.Empty;
        public string ServiceLocatorValue { get; set; } = string.Empty;
        public string ServiceId { get; set; } = string.Empty;
    }
}
