using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPAWeb.Data.Model
{
    // Անուն, որն արդեն գրանցված է cpa_service_ident-ում,
    // և տեղեկություն այն մասին, թե ՈՐՏԵՂ է գրանցված
    public class RegisteredNameInfo
    {
        // cs.SERVICE_LOCATOR_VALUE — ինքը՝ անունը
        public string LocatorValue { get; set; } = string.Empty;

        // cn.SERVICE_NAME — համարը, որին կապված է
        public string ServiceName { get; set; } = string.Empty;

        // cs.SERVICE_ID
        public string ServiceId { get; set; } = string.Empty;

        // cp.NAME — provider-ը
        public string ProviderName { get; set; } = string.Empty;
    }
}
