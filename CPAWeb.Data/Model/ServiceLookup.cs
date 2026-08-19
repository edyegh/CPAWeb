using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPAWeb.Data.Model
{
    // Համարով գտնված ծառայությունը (CPA_NUMBER + CPA_PROVIDER)
    public class ServiceLookup
    {
        // cn.SERVICE_ID — այս արժեքն է գնում procedure-ի v_service_id
        public long ServiceId { get; set; }

        // cn.SERVICE_NAME — այս արժեքն է գնում procedure-ի v_servname
        public string ServiceName { get; set; } = string.Empty;

        // cp.NAME — provider-ի անունը (միայն ցուցադրման համար)
        public string ProviderName { get; set; } = string.Empty;
    }
}
