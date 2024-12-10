namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AssetCommandAttribute
{
    public string DeviceId { get; set; }
    public string MetricKey { get; set; }
    public string Value { get; set; }
}
