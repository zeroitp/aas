namespace AasxServerDB.Repositories.AHIRepo;

using AHI.Infrastructure.Repository;

public class AHIUnitOfWork : BaseUnitOfWork, IAHIUnitOfWork
{

    public IDeviceMetricSnapshotRepository DeviceMetricSnapshots { get; private set; }

    public AHIUnitOfWork(AHIContext context,
        IDeviceMetricSnapshotRepository deviceMetricSnapshots) : base(context)
    {
        DeviceMetricSnapshots = deviceMetricSnapshots;
    }
}
