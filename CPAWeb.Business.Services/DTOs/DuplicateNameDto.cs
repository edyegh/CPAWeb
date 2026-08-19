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
    }
}
