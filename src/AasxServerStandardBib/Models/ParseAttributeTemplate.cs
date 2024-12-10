namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ParseAttributeTemplate
{
    public string ObjectType { get; set; }
    public string FileName { get; set; }
    public string TemplateId { get; set; }
    public IEnumerable<ParseAttributeRequest> UnsavedAttributes { get; set; }
}

public class ParseAttributeRequest : ValidatAttributeRequest
{
    public string Name { get; set; }
    public string MarkupName { get; set; }
    public string MetricName { get; set; }
    public string ExpressionRuntime { get; set; }
    public string DeviceMarkupName { get; set; }
    public string IntegrationMarkupName { get; set; }
    public Guid? IntegrationId { get; set; }
    public Guid? TemplateAttributeId { get; set; }
    public DateTime? UpdatedUtc { get; set; }
}