namespace AasxServerDB.Helpers;

using System;
using System.Globalization;
using System.Linq;
using AasCore.Aas3_0;
using AasxServerDB.Dto;
using global::Extensions;

public static class TimeSeriesHelper
{
    public static SubmodelElementCollection CreateEmptySnapshot(string dataType)
    {
        var snapshotSmc = new SubmodelElementCollection
        {
            DisplayName = [new LangStringNameType("en-US", "Snapshot")],
            IdShort = "Snapshot",
            Value = []
        };
        snapshotSmc.Add(new Property(valueType: MappingHelper.ToAasDataType(dataType))
        {
            IdShort = "Value",
            DisplayName = [new LangStringNameType("en-US", "Value")]
        });
        snapshotSmc.Add(new Property(valueType: DataTypeDefXsd.Long)
        {
            IdShort = "Timestamp",
            DisplayName = [new LangStringNameType("en-US", "Timestamp")]
        });
        snapshotSmc.Add(new Property(valueType: DataTypeDefXsd.Integer)
        {
            IdShort = "Quality",
            DisplayName = [new LangStringNameType("en-US", "Quality")]
        });
        return snapshotSmc;
    }

    public static string GetDataType(this ISubmodelElement sme)
    {
        if (sme is IProperty prop)
        {
            return MappingHelper.ToAhiDataType(prop.ValueType);
        }
        return sme.Extensions != null
            ? sme.Extensions.Where(x => x.Name == "DataType").Select(x => x.Value).FirstOrDefault()
            : string.Empty;
    }

    public static string GetExtensionValue(this ISubmodelElementCollection seriesSmc, string name)
    {
        return seriesSmc.Extensions != null
            ? seriesSmc.Extensions.Where(x => x.Name == name).Select(x => x.Value).FirstOrDefault()
            : string.Empty;
    }

    public static string GetQualityName(this int? q)
    {
        return q switch
        {
            192 => "Good [Non-Specific]",
            _ => null,
        };
    }

    public static TimeSeriesDto BuildSeriesDto(object value, long? timestamp = null, int? quality = null)
    {
        return new TimeSeriesDto
        {
            v = value,
            ts = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            q = quality ?? 192,
            lts = 0, // [TODO]
            l = null // [TODO]
        };
    }

    public static long ToUnixTimestamp(DateTime dateTime) => dateTime.ToUtcDateTimeOffset().ToUnixTimeMilliseconds();

    public static DateTime TimestampToDatetime(long unixTimestamp) => unixTimestamp.ToString().CutOffFloatingPointPlace().UnixTimeStampToDateTime().CutOffNanoseconds();

    public static void UpdateSnapshot(this ISubmodelElementCollection seriesSmc, TimeSeriesDto series)
    {
        var snapshotSmc = seriesSmc.FindFirstIdShortAs<ISubmodelElementCollection>("Snapshot");
        if (snapshotSmc != null)
        {
            var pSnapshot = snapshotSmc.FindFirstIdShortAs<IProperty>("Value");
            pSnapshot.Value = $"{series.v}";
            var pTimestamp = snapshotSmc.FindFirstIdShortAs<IProperty>("Timestamp");
            pTimestamp.Value = series.ts > 0
                ? series.ts.ToString(CultureInfo.InvariantCulture)
                : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
            var pQuality = snapshotSmc.FindFirstIdShortAs<IProperty>("Quality");
            pQuality.Value = series.q?.ToString(CultureInfo.InvariantCulture) ?? "192";
        }
    }

    public static TimeSeriesDto GetSnapshotTimeSeries(this ISubmodelElementCollection seriesSmc)
    {
        var dataType = seriesSmc.GetDataType();
        var snapshotSmc = seriesSmc.FindFirstIdShortAs<ISubmodelElementCollection>("Snapshot");
        if (snapshotSmc != null)
        {
            var snapshotValue = snapshotSmc.FindFirstIdShortAs<IProperty>("Value");
            var timestamp = snapshotSmc.FindFirstIdShortAs<IProperty>("Timestamp");
            var qualityProp = snapshotSmc.FindFirstIdShortAs<IProperty>("Quality");
            if (timestamp?.Value is null)
                return null;

            var tsDto = BuildSeriesDto(
            value: snapshotValue.Value?.ParseValueWithDataType(dataType, snapshotValue.Value, isRawData: false),
            timestamp: long.TryParse(timestamp.Value, CultureInfo.InvariantCulture, out var resultTs) ? resultTs : null,
            quality: int.TryParse(qualityProp.Value, out var quality) ? quality : null);
            return tsDto;
        }

        return null;
    }
}