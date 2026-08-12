using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPAWeb.Services.DTOs
{
    // Հիմնական համարը, որով ժամանակավոր աղյուսակի անունները գրանցվում են cpa_sid-ում
    public class CommitStagedRequestDto
    {
        public string Number { get; set; } = string.Empty;
    }
}
