using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPAWeb.Services.DTOs
{
    // Համարը, որից գտնվում են service_id-ն և account_id-ն ժամանակավոր
    // աղյուսակի անունները գրանցելու համար
    public class CommitStagedRequestDto
    {
        public string Number { get; set; } = string.Empty;
    }
}
