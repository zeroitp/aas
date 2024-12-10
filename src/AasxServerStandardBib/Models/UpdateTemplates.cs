namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;

public class UpdateTemplates
{    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool Deleted { get; set; }
    public int TotalMetric { get; set; }
    public ICollection<UpdateTemplatePayload> Payloads { get; set; } = new List<UpdateTemplatePayload>();
    public ICollection<UpdateTemplateBinding> Bindings { get; set; } = new List<UpdateTemplateBinding>();
}

public class UpdateTemplateBinding
{
    public int Id { get; set; }
    public Guid TemplateId { get; set; }
    public string Key { get; set; }
    public string DataType { get; set; }
    public string DefaultValue { get; set; }
}

public class UpdateTemplatePayload
{
    public int Id { get; set; }
    public Guid TemplateId { get; set; }
    public string JsonPayload { get; set; }
    public ICollection<UpdateTemplateDetails> Details { get; set; } = new List<UpdateTemplateDetails>();
}

public class UpdateTemplateDetails
{
    public int Id { get; set; }
    public int TemplatePayloadId { get; set; }
    public string Key { get; set; }
    public string Name { get; set; }
    public int KeyTypeId { get; set; }
    public string DataType { get; set; }
    //public int? UomId { get; set; }
    public string Expression { get; set; }
    public bool Enabled { get; set; }
    public Guid DetailId { get; set; } = Guid.NewGuid();
}