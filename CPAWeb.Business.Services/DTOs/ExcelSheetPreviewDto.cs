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

        // Գունավորված (նարնջագույն) տողերի առաջին սյունակի արժեքները
        public List<string> MarkedRowFirstColumnValues { get; set; } = new();
    }
}
