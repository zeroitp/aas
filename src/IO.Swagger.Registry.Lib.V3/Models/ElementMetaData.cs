namespace IO.Swagger.Registry.Lib.V3.Models;

using System.Collections.Generic;

public class BaseMetaData
{
}

public class DynamicMetaData : BaseMetaData
{
    public string DeviceId { get; set; }
    public string MetricKey { get; set; }
    public string DataType { get; set; }
}

public class AliasMetaData : BaseMetaData
{
    public string AliasPath { get; set; }
    public string RefAasId { get; set; }
    public string RefAttributeId { get; set; }
}

public class RuntimeMetaData : BaseMetaData
{
    public string DataType { get; set; }
    public string Expression { get; set; }
    public string ExpressionCompile { get; set; }
    public bool EnabledExpression { get; set; }
    public string[] TriggerAttributeIds { get; set; }
    public string TriggerAttributeId { get; set; }
}