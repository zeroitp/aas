namespace AasxServerDB.Repositories.AHIRepo;

using System;
using System.Runtime.CompilerServices;
using AasxServerDB.Dto;
using AasxServerDB.Helpers;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public class DeviceMetricSnapshotRepository : IDeviceMetricSnapshotRepository
{
    private readonly AHIContext _context;
    public DeviceMetricSnapshotRepository(AHIContext context)
    {
        _context = context;
    }

    public async Task AddCommandHistory(AssetAttributeCommandHistory history)
    {
        var query = $"INSERT INTO asset_attribute_command_histories(asset_attribute_id, value, row_version, device_id, metric_key) " +
                $"VALUES(@asset_attribute_id, @value, @row_version, @device_id, @metric_key);";

        var paramItems = new List<NpgsqlParameter> {
                new("@asset_attribute_id", history.AssetAttributeId),
                new("@value", history.Value),
                new("@row_version", history.RowVersion),
                new("@device_id", history.DeviceId),
                new("@metric_key", history.MetricKey)
        };

        await _context.Database.ExecuteSqlRawAsync(query, paramItems);
    }

    public async Task AddDeviceMetricSeries(MetricSeriesDto snapshot, string dataType = "text")
    {
        var table = string.Equals(dataType, "text", StringComparison.OrdinalIgnoreCase) ? "device_metric_series_text" : "device_metric_series";
        var query = $"INSERT INTO {table}(_ts, device_id, metric_key, value, retention_days) " +
            $"VALUES(@ts, @deviceid, @metrickey, @value, @retentiondays);";

        var paramItems = new List<NpgsqlParameter> {
                new("@ts", TimeSeriesHelper.TimestampToDatetime(snapshot.Timestamp)),
                new("@deviceid", snapshot.DeviceId),
                new("@metrickey", snapshot.MetricKey),
                new("@value", snapshot.Value),
                new("@retentiondays", snapshot.RetentionDays)
            };

        await _context.Database.ExecuteSqlRawAsync(query, paramItems);
    }

    public async Task AddDeviceMetricSnapshot(DeviceMetricSnapshot snapshot)
    {
        //TODO: handle datetime format
        var query = $@"INSERT INTO device_metric_snapshots(_ts, device_id, metric_key, value)
                                VALUES(@ts, @deviceid, @metrickey, @value)
                                ON CONFLICT (device_id, metric_key)
                                DO UPDATE SET _ts = EXCLUDED._ts, value = EXCLUDED.value WHERE device_metric_snapshots._ts < EXCLUDED._ts;
                                ";

        object[] paramItems = {
                new NpgsqlParameter("@ts", snapshot._ts),
                new NpgsqlParameter("@deviceid", snapshot.device_id),
                new NpgsqlParameter("@metrickey", snapshot.metric_key),
                new NpgsqlParameter("@value", snapshot.value),
            };

        await _context.Database.ExecuteSqlRawAsync(query, paramItems);
    }

    public async Task AddRuntimeSeries(RuntimeSeries snapshot, string dataType = "text")
    {
        //TODO: repalce this hard code value by snapshot value
        var assetId = Guid.Parse("e1627e07-7ae1-45fd-85e9-0cbf7948ca69");
        var attributeId = Guid.Parse("71323c27-db2a-4ceb-9541-e4ad6fb24d19");

        var table = string.Equals(dataType, "text", StringComparison.OrdinalIgnoreCase) ? "asset_attribute_runtime_series_text" : "asset_attribute_runtime_series";
        var query = $"INSERT INTO {table}(asset_id, asset_attribute_id, value, _ts, retention_days) " +
            $"VALUES(@asset_id, @asset_attribute_id, @value, @_ts, @retention_days);";

        var paramItems = new List<NpgsqlParameter> {
                new("@asset_id", assetId),
                new("@asset_attribute_id", attributeId),
                new("@_ts", snapshot._ts),
                new("@value", snapshot.value),
                new("@retention_days", snapshot.retention_days)
        };

        await _context.Database.ExecuteSqlRawAsync(query, paramItems);
    }

    public async Task AddStaticSeries(StaticSeries snapshot, string dataType = "text")
    {
        //TODO: repalce this hard code value by snapshot value
        var assetId = Guid.Parse("e1627e07-7ae1-45fd-85e9-0cbf7948ca69");
        var attributeId = Guid.Parse("71323c27-db2a-4ceb-9541-e4ad6fb24d19");

        var table = string.Equals(dataType, "text", StringComparison.OrdinalIgnoreCase) ? "asset_attribute_static_series_text" : "asset_attribute_static_series";
        var query = $"INSERT INTO {table}(asset_id, asset_attribute_id, value, _ts, retention_days) " +
            $"VALUES(@asset_id, @asset_attribute_id, @value, @_ts, @retention_days);";

        var paramItems = new List<NpgsqlParameter> {
                new("@asset_id", assetId),
                new("@asset_attribute_id", attributeId),
                new("@_ts", snapshot._ts),
                new("@value", snapshot.value),
                new("@retention_days", snapshot.retention_days)
        };

        await _context.Database.ExecuteSqlRawAsync(query, paramItems);
    }

    public async Task<TimeSeriesDto> GetDeviceMetricSnapshot(string deviceId, string metricKey)
    {
        try
        {
            var query = $"select * from device_metric_snapshots dms where device_id = '{deviceId}' and metric_key = '{metricKey}' order by _ts limit 1";
            var tsData = new TimeSeriesDto();
            var snapshot = await _context.Database
                .SqlQuery<DeviceMetricSnapshot>(FormattableStringFactory.Create(query)).FirstOrDefaultAsync();

            if (snapshot != null)
            {
                tsData = new TimeSeriesDto
                {
                    v = snapshot.value,
                    ts = TimeSeriesHelper.ToUnixTimestamp(snapshot._ts),
                    lts = snapshot._lts.HasValue ? snapshot._lts.Value.ToUtcDateTimeOffset().ToUnixTimeMilliseconds() : 0,
                    l = snapshot.last_good_value
                };
            }

            return tsData;
        }
        catch (Exception ex)
        {

            throw ex;
        }
        
    }
}
