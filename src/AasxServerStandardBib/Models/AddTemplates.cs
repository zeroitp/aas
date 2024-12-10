namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

public class AddTemplates
{
    public string Name { get; set; }
    public bool Deleted { get; set; }
    public IEnumerable<AddTemplatePayload> Payloads { get; set; } = new List<AddTemplatePayload>();
    public IEnumerable<AddTemplateBinding> Bindings { get; set; } = new List<AddTemplateBinding>();
}
