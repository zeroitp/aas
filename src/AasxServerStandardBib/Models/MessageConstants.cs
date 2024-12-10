namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class MessageDetailConstants
{
    public const string BROKER_HAS_BEEN_DELETED = "ERROR.API.DEVICE.IOT_HUB_HAS_BEEN_DELETED";
    public const string TIER_IS_NOT_STANDARD = "ERROR.ENTITY.INVALID.DEVICE.BASIC_TIER_NOT_SUPPORTED";
    public const string INVALID_VALUE = "ERROR.ENTITY.VALIDATION.FIELD_INVALID";
    public const string OUT_OF_RANGE_OF_DATA_TYPE = "ERROR.ENTITY.VALIDATION.OUT_OF_RANGE";
    public const string METRIC_DELETED = "ERROR.ENTITY.METRIC_HAS_BEEN_DELETED";
    public const string TOO_MANY_REQUEST = "ERROR.ENTITY.VALIDATION.TOO_MANY_REQUEST";
}
