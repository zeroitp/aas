namespace AasxServerStandardBib.Models;
using System;
using AasxServerDB.Helpers;

public class GetAssetAttributeTemplateSimpleDto
{
    public Guid Id { get; set; }
    public Guid AssetTemplateId { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
    public string Expression { get; set; }
    public bool EnableExpression { get; set; }
    public string AttributeType { get; set; }
    public string DataType { get; set; }
    public string NormalizeName => Name.NormalizeAHIName();
    public int? UomId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public virtual GetSimpleUomDto Uom { get; set; } = new GetSimpleUomDto();
    public int? DecimalPlace { get; set; }
    public bool? ThousandSeparator { get; set; }
    public int SequentialNumber { get; set; }
    public AttributeMapping Payload { get; set; } = new AttributeMapping();
}