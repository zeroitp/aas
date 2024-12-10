namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AddTemplatePayload
{
    public Guid TemplateId { get; set; }

    public string JsonPayload { get; set; }

    public ICollection<AddTemplateDetails> Details { get; set; } = new List<AddTemplateDetails>();
}
