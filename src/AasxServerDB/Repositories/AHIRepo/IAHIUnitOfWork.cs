namespace AasxServerDB.Repositories.AHIRepo;

using AHI.Infrastructure.Repository.Generic;

public interface IAHIUnitOfWork : IUnitOfWork
{
    IDeviceMetricSnapshotRepository DeviceMetricSnapshots { get; }
}
