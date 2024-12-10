namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface ITokenService
{
    Task<bool> CheckTokenAsync(string token, string prefix = null);
}
