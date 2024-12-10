namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AddTemplateBinding
{
    public Guid TemplateId { get; set; }
    public string Key { get; set; }
    public string DataType { get; set; }
    public string DefaultValue { get; set; }
}
