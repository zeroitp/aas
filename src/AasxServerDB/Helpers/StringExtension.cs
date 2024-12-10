namespace AasxServerDB.Helpers;

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Globalization;
using AasxServerDB.Dto;

public static class StringExtension
{
    public static string ToStringQuote(this string text)
    {
        return $"\"{text}\"";
    }

    public static string ToStringQuote(this Guid text)
    {
        return $"\"{text}\"";
    }

    public static bool IsNumber(this string text)
    {
        return long.TryParse(text, out _);
    }

    public static bool IsDecimalNumber(this string text)
    {
        return decimal.TryParse(text, out _);
    }

    public static bool IsDoubleNumber(this string text)
    {
        return double.TryParse(text, out _);
    }

    public static bool IsBoolean(this string text)
    {
        string[] values = { "true", "false" };
        return values.Contains(text);
    }

    public static bool IsDateTime(this string text)
    {
        return DateTime.TryParse(text, out _);
    }

    //public static bool IsTimestamp(this string text, string fieldName = ColumnFieldNameConstants.DEFAULT_VALUE)
    //{
    //    try
    //    {
    //        if (!long.TryParse(text, out var temp))
    //        {
    //            throw EntityValidationExceptionHelper.GenerateException(fieldName, ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
    //        }
    //        DateTimeOffset.FromUnixTimeMilliseconds(temp);
    //        return true;
    //    }
    //    catch (ArgumentOutOfRangeException)
    //    {
    //        throw EntityValidationExceptionHelper.GenerateException(fieldName, ExceptionErrorCode.DetailCode.ERROR_VALIDATION_OUT_OF_RANGE);
    //    }
    //}

    public static int ToNumber(this string text)
    {
        int result = 0;
        int.TryParse(text, out result);
        return result;
    }

    public static string ToLongString(double input)
    {
        // need to make sure the output is consistence in case of Infinity value (current culture may return different string than "Infinity")
        if (double.IsInfinity(input))
            return input.ToString(System.Globalization.CultureInfo.InvariantCulture);

        string strOrig = input.ToString();
        string str = strOrig.ToUpper();

        // if string representation was collapsed from scientific notation, just return it:
        if (!str.Contains("E"))
            return strOrig;

        bool negativeNumber = false;

        if (str[0] == '-')
        {
            str = str.Remove(0, 1);
            negativeNumber = true;
        }

        string sep = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        char decSeparator = sep.ToCharArray()[0];

        string[] exponentParts = str.Split('E');
        string[] decimalParts = exponentParts[0].Split(decSeparator);

        // fix missing decimal point:
        if (decimalParts.Length == 1)
            decimalParts = new string[] { exponentParts[0], "0" };

        int exponentValue = int.Parse(exponentParts[1]);

        string newNumber = decimalParts[0] + decimalParts[1];

        string result;

        if (exponentValue > 0)
        {
            result =
                newNumber +
                GetZeros(exponentValue - decimalParts[1].Length);
        }
        else // negative exponent
        {
            result =
                "0" +
                decSeparator +
                GetZeros(exponentValue + decimalParts[0].Length) +
                newNumber;

            result = result.TrimEnd('0');
        }

        if (negativeNumber)
            result = "-" + result;
        return result;
    }

    private static string GetZeros(int zeroCount)
    {
        if (zeroCount < 0)
            zeroCount = Math.Abs(zeroCount);

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < zeroCount; i++)
            sb.Append("0");

        return sb.ToString();
    }

    public static bool CanParseResultWithDataType(this string value, string dataType)
    {
        switch (dataType)
        {
            case DataTypeConstants.TYPE_BOOLEAN:
                return bool.TryParse(value, out _);

            case DataTypeConstants.TYPE_DOUBLE:
                return double.TryParse(value, out var v) && !double.IsNaN(v);

            case DataTypeConstants.TYPE_INTEGER:
                return int.TryParse(value, out int _);

            case "text":
                return Regex.IsMatch(value, "^(?=.{0,255}$)");

            default:
                return true;
        }
    }

    public static string FormatValueByDataType(this string value, string dataType)
    {
        if (!string.IsNullOrEmpty(value))
        {
            switch (dataType)
            {
                case DataTypeConstants.TYPE_BOOLEAN:
                    if (bool.TryParse(value, out var boolValue))
                    {
                        return boolValue.ToString().ToLowerInvariant();
                    }
                    return value;

                case DataTypeConstants.TYPE_DOUBLE:
                {
                    if (double.TryParse(value, out var doubleValue))
                    {
                        var result = doubleValue.ToString();
                        if (result.Contains('E'))
                        {
                            result = double.Parse(value).ToString("0.#################");
                        }
                        return result;
                    }
                    return value;
                }
                // int and text type should have exact data without additional handling
            }
        }
        return value;
    }

    public static string UpperCaseFirstChar(this string text)
    {
        return Regex.Replace(text, "^[a-z]", m => m.Value.ToUpper());
    }

    //public static object ParseValue(this string value, string dataType, int textLengthLimit = 255)
    //{
    //    string pattern = textLengthLimit == int.MaxValue ? "^(?=.*$)" : $"^(?=.{{0,{textLengthLimit}}}$)";
    //    switch (dataType)
    //    {
    //        case DataTypeConstants.TYPE_BOOLEAN:
    //            return bool.Parse(value);

    //        case DataTypeConstants.TYPE_DOUBLE:
    //        {
    //            var parsedValue = double.Parse(value);
    //            if (double.IsInfinity(parsedValue))
    //                throw EntityValidationExceptionHelper.GenerateException(ColumnFieldNameConstants.VALUE, ExceptionErrorCode.DetailCode.ERROR_VALIDATION_OUT_OF_RANGE);
    //            return parsedValue;
    //        }
    //        case DataTypeConstants.TYPE_INTEGER: // choose integer data type but response return type is float is still true
    //            return int.Parse(value);

    //        case DataTypeConstants.TYPE_TEXT:
    //            if (Regex.IsMatch(value, pattern))
    //                return value;
    //            throw EntityValidationExceptionHelper.GenerateException(ColumnFieldNameConstants.VALUE, ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH);
    //    }
    //    throw EntityValidationExceptionHelper.GenerateException(ColumnFieldNameConstants.DATA_TYPE, ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
    //}

    public static string NormalizeAHIName(this string str)
    {
        if (str != null)
        {
            StringBuilder sb = new StringBuilder("AHI_");
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
                else
                {
                    //replace by underscore
                    sb.Append('_');
                }
            }
            return sb.ToString();
        }
        return str;
    }

    public static (string Operation, int OffsetHour, int OffsetMinutes, string PostgresOffsetQuery, string PostgresOffsetQueryReverse) TimezoneOffsetToOffsetTime(this string timezoneOffset)
    {
        var hour = 0;
        var minutes = 0;
        var operation = "+";
        if (!string.IsNullOrEmpty(timezoneOffset) && (timezoneOffset.StartsWith("+") || timezoneOffset.StartsWith("-")))
        {
            if (timezoneOffset.StartsWith("-"))
            {
                operation = "-";
            }
            timezoneOffset = timezoneOffset.Replace(operation, "");
            int.TryParse(timezoneOffset.Split(':')[0], out hour);
            int.TryParse(timezoneOffset.Split(':')[1], out minutes);
        }
        var postgresQuery = $"{operation} interval '{hour} hours {minutes} minutes'";
        var postgresQueryReverse = $"{(operation == "+" ? "-" : "+")} interval '{hour} hours {minutes} minutes'";
        return (operation, hour, minutes, postgresQuery, postgresQueryReverse);
    }

    public static string GetCacheKey(this string cacheKey, params object[] args)
    {
        return string.Format(cacheKey, args);
    }

    public static long GetUnixTimestampPadding(this string timegrain)
    {
        if (string.IsNullOrEmpty(timegrain))
        {
            return 0;
        }
        var splitTimegrain = timegrain.Trim().Split(' ');
        if (!splitTimegrain.Any() || splitTimegrain.Length < 2)
        {
            return 0;
        }

        if (!long.TryParse(splitTimegrain.First(), out var period))
        {
            return 0;
        }

        switch (splitTimegrain.Last())
        {
            case "year":
            case "years":
                return period * 365 * 24 * 60 * 60 * 1000;
            case "month":
            case "months":
                return period * 31 * 24 * 60 * 60 * 1000;
            case "week":
            case "weeks":
                return period * 7 * 24 * 60 * 60 * 1000;
            case "day":
            case "days":
                return period * 24 * 60 * 60 * 1000;
            case "hour":
            case "hours":
                return period * 60 * 60 * 1000;
            case "minute":
            case "minutes":
                return period * 60 * 1000;
            case "second":
            case "seconds":
                return period * 1000;
            default:
                return 0;
        }
    }

    public static string CutOffFloatingPointPlace(this string timestamp)
    {
        if (double.TryParse(timestamp, out double unixDoubleTimestamp))
        {
            return Convert.ToInt64(unixDoubleTimestamp).ToString();
        }
        return timestamp;
    }

    public static DateTime UnixTimeStampToDateTime(this string unixTimeStamp)
    {
        if (long.TryParse(unixTimeStamp, out var result))
        {
            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(result).DateTime;
            }
            catch
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(result).DateTime;
            }
        }

        if (DateTimeOffset.TryParse(unixTimeStamp, out var result2))
        {
            return result2.DateTime;
        }

        if (!DateTime.TryParseExact(unixTimeStamp, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var _) && DateTime.TryParse(unixTimeStamp, out var result4))
        {
            return result4;
        }

        throw new Exception("Can not parse DateTime for string - " + unixTimeStamp);
    }

    public static DateTime CutOffNanoseconds(this DateTime dateTime)
    {
        var data = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond, dateTime.Kind);
        return DateTime.SpecifyKind(data, DateTimeKind.Utc);
    }

    public static DateTimeOffset ToUtcDateTimeOffset(this DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(dateTime),
            DateTimeKind.Local => new DateTimeOffset(dateTime.ToUniversalTime()),
            _ => new DateTimeOffset(dateTime, TimeSpan.Zero)
        };
    }
}
