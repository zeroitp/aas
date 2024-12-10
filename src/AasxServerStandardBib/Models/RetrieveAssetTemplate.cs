namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class RetrieveAssetTemplate
{
    public string Data { get; set; }
    public IDictionary<string, object> AdditionalData { get; set; }
    public string Upn { get; set; }
}
