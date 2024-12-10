namespace IO.Swagger.Registry.Lib.V3.Models;

public class TemplateAttributeUpdatedMessage
{
    public string AASIdShort { get; set; }
    public string AttributeIdShort { get; set; }
    public AttributeUpdatedType Type { get; set; }
}

public enum AttributeUpdatedType
{
    Add,
    Edit,
    Remove
}
