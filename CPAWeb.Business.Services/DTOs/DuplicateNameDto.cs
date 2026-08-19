using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPAWeb.Services.DTOs
{
    // Անուն, որն արդեն գրանցված էր ժամանակավոր աղյուսակ ավելացնելու պահին
    public class DuplicateNameDto
    {
        public string Name { get; set; } = string.Empty;

        // Որտեղից է եկել՝ sheet-ի անունը կամ "add new name"
        public string Source { get; set; } = string.Empty;

        public DateTime DetectedAt { get; set; }

        // ՈՐՏԵՂ է արդեն գրանցված
        public string ServiceName { get; set; } = string.Empty;   // համարը (cn.SERVICE_NAME)
        public string ServiceId { get; set; } = string.Empty;     // cs.SERVICE_ID
        public string ProviderName { get; set; } = string.Empty;  // cp.NAME

        // "374088006492 (service_id 2582)" — UI-ի և .txt-ի համար
        public string RegisteredAt
        {
            get
            {
                var parts = new List<string>();

                if (!string.IsNullOrWhiteSpace(ServiceName))
                    parts.Add(ServiceName);

                if (!string.IsNullOrWhiteSpace(ServiceId))
                    parts.Add($"service_id {ServiceId}");

                return parts.Count > 0 ? string.Join(" ", parts) : "-";
            }
        }
    }
}
