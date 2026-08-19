using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPAWeb.Services.DTOs
{
    // "add new name" կոճակի արդյունքը՝ ո՞ր service_id-ով և account_id-ով է գրանցվել անունը
    public class AddNameResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        // Համարից գտնված ծառայությունը
        public long ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;

        // Ծառայությունից գտնված հաշիվը
        public long AccountId { get; set; }

        // Եթե ծառայությանը կապված է մեկից ավելի account, բոլորը ցույց ենք տալիս UI-ում
        public List<long> AccountCandidates { get; set; } = new();

        // Քանի անուն է իրականում գրանցվել cpa_service_ident-ում
        public int RegisteredCount { get; set; }

        // Անուններ, որոնք արդեն գրանցված էին
        public List<string> AlreadyRegistered { get; set; } = new();
    }
}
