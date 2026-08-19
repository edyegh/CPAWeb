using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPAWeb.Services.DTOs
{
    // "save sheet data" կոճակի պատասխանը՝ ինչքան անուն է դրվել ժամանակավոր աղյուսակում
    public class StageSheetResultDto
    {
        public string SheetName { get; set; } = string.Empty;

        // Քանի Name է գրանցվել edyeghiazaryan_insertvalue-ում
        public int StagedCount { get; set; }

        // Sheet-ի անունից վերցված համարը ("Nikita 5124" -> "5124"), UI-ում կարելի է փոխել
        public string? SuggestedNumber { get; set; }

        // Անուններ, որոնք արդեն գրանցված են
        public List<string> DuplicateNames { get; set; } = new();
    }
}
