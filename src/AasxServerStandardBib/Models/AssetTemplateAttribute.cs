namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

public class AssetTemplateAttribute
{
    public Guid Id { get; set; }
    public Guid? AssetId { get; set; }
    public string? Name { get; set; }
    public string? Value { get; set; }
    // public bool EnabledExpression { get; set; } = true;
    // public string Expression { get; set; }
    public string? AttributeType { get; set; }
    public string? DataType { get; set; }
    public int? UomId { get; set; }
    public int? DecimalPlace { get; set; }
    public bool? ThousandSeparator { get; set; }
    public int SequentialNumber { get; set; } = 1;
    public AttributeMapping Payload { get; set; } = new AttributeMapping();
    public AssetTemplateAttribute()
    {
        Id = Guid.NewGuid();
    }
}