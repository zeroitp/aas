using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AasxServerDB.Dto;

[ValidateNever]
public class TimeSeriesDto
{
    public long ts { get; set; }
    public object v { get; set; }
    public object l { get; set; }
    public long? lts { get; set; }
    public int? q { get; set; }
}

public class MetricSeriesDto
{
    public long Timestamp { get; set; }
    public string DeviceId { get; set; }
    public string MetricKey { get; set; }
    public object Value { get; set; }
    public int RetentionDays { get; set; }
    public int? Quality { get; set; }
}

public class AttributeSnapshot
{
    public DateTime Timestamp { get; set; }
    public Guid AASIdShort { get; set; }
    public int AASId { get; set; }
    public Guid AttributeIdShort { get; set; }
    public int SMEId { get; set; }
    public string Value { get; set; }
}

public class DeviceMetricSnapshot
{
    public string device_id { get; set; }
    public string metric_key { get; set; }
    public string value { get; set; }
    public DateTime _ts { get; set; }
    public string? last_good_value { get; set; }
    public DateTime? _lts { get; set; }
}

public class RuntimeSeries
{
    public DateTime _ts { get; set; }
    public Guid asset_id { get; set; }
    public Guid asset_attribute_id { get; set; }
    public object? value { get; set; }
    public int? retention_days { get; set; }
}

public class StaticSeries
{
    public DateTime _ts { get; set; }
    public Guid asset_id { get; set; }
    public Guid asset_attribute_id { get; set; }
    public object? value { get; set; }
    public int? retention_days { get; set; }
}