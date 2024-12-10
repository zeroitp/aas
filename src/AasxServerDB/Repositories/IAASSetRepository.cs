namespace AasxServerDB.Repositories;
using System.Threading.Tasks;
using AasxServerDB.Entities;
using AHI.Infrastructure.Repository.Generic;

public interface IAASSetRepository : IRepository<AASSet, int>
{
    Task ClearDB();
}
