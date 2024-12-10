namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxServerDB.Dto;
using MimeKit;
using ScottPlot.Renderable;

public class AttributeTemplate
{
    public AttributeTemplate()
    {
    }
    public AttributeTemplate(Guid? id, string attributeType, Guid? channelId, string channelMarkup, string deviceMarkup, string deviceTemplate)
    {
        AttributeId = id;
        AttributeType = attributeType;
        ChannelId = channelId;
        ChannelMarkup = channelMarkup;
        DeviceMarkup = deviceMarkup;
        DeviceTemplate = deviceTemplate;
    }

    public Guid? AttributeId { get; set; } = Guid.NewGuid();
    public string AttributeName { get; set; }

    public AssetAttributeType? Type { get; private set; }
    public string AttributeType
    {
        get => Type?.ToString()?.ToLower();
        set
        {
            if (Enum.TryParse<AssetAttributeType>(value, true, out var type))
                Type = type;
            else
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                var exception = new ArgumentException(ParseValidation.PARSER_INVALID_DATA);
                exception.Data["validationInfo"] = new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.TYPE },
                        { ErrorProperty.ERROR_PROPERTY_VALUE, value }
                    };
                throw exception;
            }
        }
    }

    private string _deviceTemplate;
    public string DeviceTemplate
    {
        get => _deviceTemplate;
        set
        {
            if (Type == AssetAttributeType.DYNAMIC || Type == AssetAttributeType.COMMAND || Type == AssetAttributeType.INTEGRATION)
                _deviceTemplate = value;
        }
    }

    public Guid? DeviceTemplateId { get; set; }

    private string _channel;
    public string Channel
    {
        get => _channel;
        set
        {
            if (Type == AssetAttributeType.INTEGRATION)
                _channel = value;
        }
    }

    public Guid? ChannelId { get; set; }

    private string _channelMarkup;
    public string ChannelMarkup
    {
        get => _channelMarkup;
        set
        {
            if (Type == AssetAttributeType.INTEGRATION)
                _channelMarkup = value;
        }
    }

    private string _device;
    public string Device
    {
        get => _device;
        set
        {
            if (Type == AssetAttributeType.INTEGRATION)
                _device = value;
        }
    }

    private string _deviceMarkup;
    public string DeviceMarkup
    {
        get => _deviceMarkup;
        set
        {
            if (Type == AssetAttributeType.DYNAMIC || Type == AssetAttributeType.INTEGRATION || Type == AssetAttributeType.COMMAND)
                _deviceMarkup = value;
        }
    }

    private string _metric;
    public string Metric
    {
        get => _metric;
        set
        {
            if (Type == AssetAttributeType.DYNAMIC || Type == AssetAttributeType.INTEGRATION || Type == AssetAttributeType.COMMAND)
                _metric = value;
        }
    }

    private string _dataType;
    public string DataType
    {
        get => _dataType;
        set
        {
            if (Type == AssetAttributeType.STATIC)
            {
                if (string.IsNullOrEmpty(value))
                    return;

                var validData = DataTypeExtensions.IsDataTypeForAttribute(value);
                if (!validData)
                {
                    var exception = new ArgumentException(ParseValidation.PARSER_INVALID_DATA);
                    exception.Data["validationInfo"] = new Dictionary<string, object>
                        {
                            { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.DATA_TYPE },
                            { ErrorProperty.ERROR_PROPERTY_VALUE, value }
                        };
                    throw exception;
                }
            }
            _dataType = value?.ToLower();
        }
    }

    public string _value;
    public string Value
    {
        get => _value;
        set
        {
            if (Type == AssetAttributeType.STATIC)
                _value = value;
        }
    }

    //public string DataType { get; set; }

    public string Uom { get; set; }
    public int? UomId { get; set; }
    public string DecimalPlace { get; set; }
    public string ThousandSeparator { get; set; }
    public Guid? TriggerAssetAttributeId { get; set; }
    public string TriggerAssetAttribute { get; set; }
    private string _enabledExpression;
    public string EnabledExpression
    {
        get => _enabledExpression;
        set
        {
            if (Type == AssetAttributeType.RUNTIME)
                _enabledExpression = value;
        }
    }
    private string _expression;
    public string Expression
    {
        get => _expression;
        set
        {
            if (Type == AssetAttributeType.RUNTIME)
                _expression = value;
        }
    }
    public Uom UomDetail { get; set; }
    public string MetricName { get; set; }
    public DateTime? UpdatedUtc { get; set; }

    public bool IsStaticAttribute => string.Equals(AttributeType, AttributeTypeConstants.TYPE_STATIC, StringComparison.InvariantCultureIgnoreCase);
    public bool IsAliasAttribute => string.Equals(AttributeType, AttributeTypeConstants.TYPE_ALIAS, StringComparison.InvariantCultureIgnoreCase);
    public bool IsDynamicAttribute => string.Equals(AttributeType, AttributeTypeConstants.TYPE_DYNAMIC, StringComparison.InvariantCultureIgnoreCase);
    public bool IsRuntimeAttribute => string.Equals(AttributeType, AttributeTypeConstants.TYPE_RUNTIME, StringComparison.InvariantCultureIgnoreCase);
    public bool IsCommandAttribute => string.Equals(AttributeType, AttributeTypeConstants.TYPE_COMMAND, StringComparison.InvariantCultureIgnoreCase);
    public bool IsIntegrationAttribute => string.Equals(AttributeType, AttributeTypeConstants.TYPE_INTEGRATION, StringComparison.InvariantCultureIgnoreCase);

    public void SetDefaultValue(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(Channel):
                Channel = null;
                ChannelMarkup = null;
                DeviceTemplateId = null;
                DeviceTemplate = null;
                Device = null;
                DeviceMarkup = null;
                Metric = null;
                break;
            case nameof(Device):
                DeviceTemplateId = null;
                DeviceTemplate = null;
                Metric = null;
                DeviceMarkup = null;
                break;
            case nameof(Metric):
                Metric = null;
                break;
            case nameof(Uom):
                Uom = null;
                break;
            case nameof(EnabledExpression):
                EnabledExpression = FormatDefaultConstants.ATTRIBUTE_RUNTIME_ENABLE_EXPRESSION_DEFAULT;
                Expression = null;
                TriggerAssetAttribute = null;
                break;
        }
    }
}

public enum AssetAttributeType
{
    STATIC,
    DYNAMIC,
    RUNTIME,
    INTEGRATION,
    COMMAND,
    ALIAS
}

public static class DataTypeExtensions
{
    private static readonly string[] METRIC_SERIES_NUMERIC_TYPES = new string[] { DataTypeConstants.TYPE_BOOLEAN, DataTypeConstants.TYPE_DOUBLE, DataTypeConstants.TYPE_INTEGER };
    private static readonly string[] METRIC_SERIES_TEXT_TYPES = new string[] { DataTypeConstants.TYPE_TEXT, DataTypeConstants.TYPE_DATETIME };

    /// <summary>
    /// Return the list of all data type using for Asset's attributes.
    /// </summary>
    /// <seealso cref="IsDataTypeForAttribute">Please consider using this function for checking if Data Type is valid (or not).</seealso>
    public static readonly string[] ATTRIBUTE_DATA_TYPES = new string[] { DataTypeConstants.TYPE_BOOLEAN, DataTypeConstants.TYPE_DOUBLE, DataTypeConstants.TYPE_INTEGER, DataTypeConstants.TYPE_TEXT, DataTypeConstants.TYPE_DATETIME };

    /// <summary>
    /// Return the list of all data type using for Asset Template's attributes.
    /// </summary>
    /// <seealso cref="IsDataTypeForTemplateAttribute">Please consider using this function for checking if Data Type is valid (or not).</seealso>
    public static readonly string[] TEMPLATE_ATTRIBUTE_DATA_TYPES = new string[] { DataTypeConstants.TYPE_BOOLEAN, DataTypeConstants.TYPE_DOUBLE, DataTypeConstants.TYPE_INTEGER, DataTypeConstants.TYPE_TEXT };


    public static bool IsNumericTypeSeries(string dataType)
    {
        return CompareDataType(METRIC_SERIES_NUMERIC_TYPES, dataType);
    }
    public static bool IsTextTypeSeries(string dataType)
    {
        return CompareDataType(METRIC_SERIES_TEXT_TYPES, dataType);
    }
    public static bool IsDataTypeForAttribute(string dataType)
    {
        return CompareDataType(ATTRIBUTE_DATA_TYPES, dataType);
    }
    public static bool IsDataTypeForTemplateAttribute(string dataType)
    {
        return CompareDataType(TEMPLATE_ATTRIBUTE_DATA_TYPES, dataType);
    }


    private static bool CompareDataType(string[] listDataTypes, string dataType)
    {
        return listDataTypes.Any(x => string.Equals(dataType, x, System.StringComparison.InvariantCultureIgnoreCase));
    }
}

public static class ParseValidation
{
    public const string PARSER_GENERAL_INVALID_VALUE = "FILE.PARSE_ERROR.GENERAL_INVALID";
    public const string PARSER_MAX_LENGTH = "FILE.PARSE_ERROR.MAX_LENGTH";
    public const string PARSER_REQUIRED = "FILE.PARSE_ERROR.REQUIRED";
    public const string PARSER_MISSING_COLUMN = "FILE.PARSE_ERROR.MISSING_COLUMN";
    public const string PARSER_MISSING_ATTRIBUTE_TYPE = "FILE.PARSE_ERROR.MISSING_ATTRIBUTE_TYPE";
    public const string PARSER_DUPLICATED_ATTRIBUTE_NAME = "FILE.PARSE_ERROR.DUPLICATED_ATTRIBUTE_NAME";
    public const string PARSER_MANDATORY_FIELDS_REQUIRED = "FILE.PARSE_ERROR.MANDATORY_FIELDS_REQUIRED";
    public const string PARSER_DEPENDENCES_REQUIRED = "FILE.PARSE_ERROR.DEPENDENCES_REQUIRED";
    public const string PARSER_REFERENCES_DATA_NOT_EXISTS = "FILE.PARSE_ERROR.REFERENCES_DATA_NOT_EXISTS";
    public const string PARSER_INVALID_DATA = "FILE.PARSE_ERROR.INVALID_DATA";
}

public static class ErrorProperty
{
    public const string ERROR_PROPERTY_NAME = "PropertyName";
    public const string ERROR_PROPERTY_VALUE = "PropertyValue";
    public static class Device
    {
        public const string ID = "Identifier";
        public const string NAME = "Name";
        public const string TEMPLATE = "Template";
        public const string RETENTION_DAYS = "Retention Day";
        public const string BROKER_NAME = "Broker";
        public const string SAS_TOKEN_DURATION = "SAS Token Duration";
        public const string TOKEN_DURATION = "Token Duration";
        public const string TELEMETRY_TOPIC = "Telemetry Topic";
        public const string COMMAND_TOPIC = "Command Topic";
    }
    public static class DeviceTemplate
    {
        public const string NAME = "Name";
        public const string DATA_TYPE = "Data Type";
        public const string PAYLOAD = "Payload";
        public const string DETAIL = "Detail";
        public const string DETAILS = "Details";
        public const string KEY = "Key";
        public const string KEY_TYPE = "Key Type";
        public const string EXPRESSION = "Expression";
        public const string DEFAULT_VALUE = "Default Value";
    }
    public static class AssetTemplate
    {
        public const string NAME = "Name";
        public const string DATA_TYPE = "Data Type";
        public const string ATTRIBUTE_NAME = "Attribute Name";
        public const string DEVICE_TEMPLATE = "Device Template";
        public const string CHANNEL = "Channel";
        public const string MARKUP_CHANNEL = "Markup Channel";
        public const string DEVICE = "DeviceID";
        public const string MARKUP_DEVICE = "Markup DeviceID";
        public const string MARKUP_TRIGGER_ASSET = "Markup Trigger Asset ID";
        public const string METRIC = "Metric";
        public const string UOM = "UoM";
        public const string VALUE = "Value";
        public const string ASSET_TEMPLATE = "Asset Template";
        public const string DECIMAL_PLACES = "Decimal Places";
        public const string THOUSAND_SEPARATOR = "Thousand Separator";
    }
    public static class AssetAttribute
    {
        public const string ATTRIBUTE_NAME = "Attribute Name";
        public const string ATTRIBUTE_TYPE = "Type";
        public const string DEVICE_ID = "Device ID";
        public const string CHANNEL = "Channel";
        public const string METRIC = "Metric";
        public const string VALUE = "Value";
        public const string DATA_TYPE = "Data Type";
        public const string ALIAS_ASSET = "Alias Asset";
        public const string ALIAS_ATTRIBUTE = "Alias Attribute";
        public const string ENABLED_EXPRESSION = "Enabled Expression";
        public const string EXPRESSION = "Expression";
        public const string TRIGGER_ATTRIBUTE = "Trigger Attribute";
        public const string UOM = "UoM";
        public const string DECIMAL_PLACES = "Decimal Place";
        public const string THOUSAND_SEPARATOR = "Thousand Separator";
    }
    public static class Uom
    {
        public const string NAME = "Name";
        public const string LOOKUP = "Category";
        public const string ABBREVIATION = "Abbreviation";
        public const string REF_NAME = "Reference UoM";
        public const string REF_FACTOR = "Factor";
        public const string CANONICAL_FACTOR = "Canonical Factor";
        public const string REF_OFFSET = "Offset";
        public const string CANONICAL_OFFSET = "Canonical Offset";
    }
    public static class AttributeTemplate
    {
        public const string ATTRIBUTE_NAME = "Attribute Name";
        public const string TYPE = "Type";
        public const string DEVICE_TEMPLATE = "Device Template/ID";
        public const string CHANNEL = "Channel";
        public const string MARKUP_CHANNEL = "Channel Markup";
        public const string DEVICE_ID = "Device";
        public const string MARKUP_DEVICE = "Device Markup";
        public const string METRIC = "Metric";
        public const string VALUE = "Value";
        public const string DATA_TYPE = "Data Type";
        public const string ENABLED_EXPRESSION = "Enabled Expression";
        public const string EXPRESSION = "Expression";
        public const string UOM = "UoM";
        public const string DECIMAL_PLACE = "Decimal Place";
        public const string THOUSAND_SEPARATOR = "Thousand Separator";
        public const string TRIGGER_ATTRIBUTE = "Trigger Attribute";
    }
}

public class Uom
{
    #region Properties

    public int? Id { get; set; }

    public string Name { get; set; }

    public string Lookup { get; set; }

    public string Abbreviation { get; set; }

    public double? CanonicalFactor { get; set; }

    public double? CanonicalOffset { get; set; }

    public double? RefFactor { get; set; }

    public double? RefOffset { get; set; }

    public int? RefId { get; set; }

    public string RefName { get; set; }


    #endregion
}