namespace AasxServerDB.Repositories;

using AHI.Infrastructure.Repository.Generic;

public interface IAASUnitOfWork : IUnitOfWork
{
    IAASSetRepository AASSets { get; }
    ISMSetRepository SMSets { get; }
    ISMESetRepository SMESets { get; }
    //ISValueSetRepository SValueSets { get; }
    //IIValueSetRepository IValueSets { get; }
    //IDValueSetRepository DValueSets { get; }
    //IOValueSetRepository OValueSets { get; }
}
