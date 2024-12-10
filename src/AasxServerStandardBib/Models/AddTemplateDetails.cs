namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AddTemplateDetails
{
    public int TemplatePayloadId { get; set; }
    public string Key { get; set; }
    public string Name { get; set; }
    public int KeyTypeId { get; set; }
    public string DataType { get; set; }
    public string Expression { get; set; }
    public bool Enabled { get; set; }
    public Guid DetailId { get; set; } = Guid.NewGuid();
}
