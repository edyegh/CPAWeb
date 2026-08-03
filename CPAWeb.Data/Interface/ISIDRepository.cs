using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CPAWeb.Data.Model;

namespace CPAWeb.Data.Interface
{
    public interface ISIDRepository
    {
        Task<SID?> GetSIDByNameAsync(string name);
        Task<bool> AddSIDAsync(SID sid);
    }
}
