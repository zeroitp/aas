namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class UnsaveAttribute
{
    public string MetricKey { get; set; }
    public string AttributeType { get; set; }
    public Guid? DeviceTemplateId { get; set; }
    public Guid? ChannelId { get; set; }
    public string DeviceId { get; set; }
    public string DeviceIdIntegration { get; set; }
    public string MetricKeyIntegration { get; set; }
    public int UomId { get; set; }
    public Guid? AliasAssetId { get; set; }
    public Guid AttributeId { get; set; }
    public Guid? AliasAttributeId { get; set; }
    public Guid? TriggerAttributeId { get; set; }
    public string Expression { get; set; }
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string Value { get; set; }
    public string DataType { get; set; }
    public bool EnabledExpression { get; set; }
    public bool IsTriggerVisibility { get; set; }
    public string Name { get; set; }
    public string MarkupName { get; set; }
    public string MetricName { get; set; }
    public string ExpressionRuntime { get; set; }
    public string DeviceMarkupName { get; set; }
    public string IntegrationMarkupName { get; set; }
    public Guid? IntegrationId { get; set; }
    public Guid? TemplateAttributeId { get; set; }
    public DateTime UpdatedUtc { get; set; }
}