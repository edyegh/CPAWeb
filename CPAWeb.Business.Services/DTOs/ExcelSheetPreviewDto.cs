using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPAWeb.Services.DTOs
{
    public class ExcelSheetPreviewDto
    {
        public string SheetName { get; set; } = string.Empty;
        public List<string> YellowRowFirstColumnValues { get; set; } = new();
    }
}
