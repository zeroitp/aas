namespace I.Swagger.Registry.Lib.V3.Services;

using System;
using System.Threading.Tasks;
using AasxServerDB.Dto;
using AasxServerDB.Repositories.AHIRepo;
using AasxServerDB.Helpers;

public class TimeSeriesService(IAHIUnitOfWork _unitOfWork)
{
    public Task<TimeSeriesDto> GetDeviceMetricSnapshot(string deviceId, string metricKey)
    {
        return _unitOfWork.DeviceMetricSnapshots.GetDeviceMetricSnapshot(deviceId, metricKey);
    }

    public async Task AddStaticSeries(Guid assetId, Guid attributeId, TimeSeriesDto series, string dataType = "text")
    {
        if (series.v != null)
        {
            //var snapshot = await GetSnapshot(assetId, attributeId);
            //if (snapshot == null || TimeSeriesHelper.TimestampToDatetime(snapshot.ts) < TimeSeriesHelper.TimestampToDatetime(series.ts))
            //{
            //    await AddSnapshot(assetId, attributeId, TimeSeriesHelper.TimestampToDatetime(series.ts), series.v.ToString());
            //}

            await AddStaticSeries(new StaticSeries
            {
                asset_attribute_id = attributeId,
                asset_id = assetId,
                retention_days = 90, //TODO
                value = series.v,
                _ts = TimeSeriesHelper.TimestampToDatetime(series.ts)
            }, dataType);
        }
    }

    private async Task AddStaticSeries(StaticSeries snapshot, string dataType = "text")
    {
        await _unitOfWork.DeviceMetricSnapshots.AddStaticSeries(snapshot, dataType);
    }

    public async Task AddRuntimeSeries(Guid assetId, Guid attributeId, TimeSeriesDto series, string dataType = "text")
    {
        //var snapshot = await GetSnapshot(assetId, attributeId);
        //if (snapshot == null || TimeSeriesHelper.TimestampToDatetime(snapshot.ts) < TimeSeriesHelper.TimestampToDatetime(series.ts))
        //{
        //    await AddSnapshot(assetId, attributeId, TimeSeriesHelper.TimestampToDatetime(series.ts), series.v.ToString());
        //}

        await AddRuntimeSeries(new RuntimeSeries
        {
            asset_attribute_id = attributeId,
            asset_id = assetId,
            retention_days = 90, //TODO
            value = series.v,
            _ts = TimeSeriesHelper.TimestampToDatetime(series.ts)
        }, dataType);
    }

    private async Task AddRuntimeSeries(RuntimeSeries snapshot, string dataType = "text")
    {
        await _unitOfWork.DeviceMetricSnapshots.AddRuntimeSeries(snapshot, dataType);
    }

    public async Task AddDeviceMetricSnapshot(DeviceMetricSnapshot snapshot)
    {
        await _unitOfWork.DeviceMetricSnapshots.AddDeviceMetricSnapshot(snapshot);
    }

    public async Task AddDeviceMetricSeries(string deviceId, string metricKey, TimeSeriesDto series, string dataType = "text")
    {
        var snapshot = await GetDeviceMetricSnapshot(deviceId, metricKey);
        if (snapshot == null || TimeSeriesHelper.TimestampToDatetime(snapshot.ts) < TimeSeriesHelper.TimestampToDatetime(series.ts))
        {
            await AddDeviceMetricSnapshot(new DeviceMetricSnapshot
            {
                device_id = deviceId,
                metric_key = metricKey,
                value = series.v.ToString(),
                _ts = TimeSeriesHelper.TimestampToDatetime(series.ts),
                //last_good_value = series.ToString(), // TODO
                //_lts = series.lts //TODO
            });
        }

        await AddDeviceMetricSeries(new MetricSeriesDto
        {
            DeviceId = deviceId,
            MetricKey = metricKey,
            Quality = series.q ?? 192, //TODO
            RetentionDays = 90, //TODO
            Timestamp = series.ts,
            Value = series.v.ToString()
        }, dataType);
    }

    private async Task AddDeviceMetricSeries(MetricSeriesDto snapshot, string dataType = "text")
    {
        await _unitOfWork.DeviceMetricSnapshots.AddDeviceMetricSeries(snapshot, dataType);
    }

    public async Task AddCommandHistory(AssetAttributeCommandHistory history)
    {
        await _unitOfWork.DeviceMetricSnapshots.AddCommandHistory(history);
    }
}