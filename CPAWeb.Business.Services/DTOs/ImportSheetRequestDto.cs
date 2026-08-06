using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPAWeb.Services.DTOs
{
    public class ImportSheetRequestDto
    {
        public string SheetName { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
    }
}
