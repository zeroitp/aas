namespace AasxServerDB.Repositories.AHIRepo;
using System.Threading.Tasks;
using AasxServerDB.Dto;

public interface IDeviceMetricSnapshotRepository
{
    Task<TimeSeriesDto> GetDeviceMetricSnapshot(string deviceId, string metricKey);
    Task AddStaticSeries(StaticSeries snapshot, string dataType = "text");
    Task AddRuntimeSeries(RuntimeSeries snapshot, string dataType = "text");
    Task AddDeviceMetricSnapshot(DeviceMetricSnapshot snapshot);
    Task AddDeviceMetricSeries(MetricSeriesDto snapshot, string dataType = "text");
    Task AddCommandHistory(AssetAttributeCommandHistory history);
}
