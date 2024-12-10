namespace AasxServerDB.Repositories;
using System.Threading.Tasks;
using AasxServerDB.Entities;
using AHI.Infrastructure.Repository.Generic;

public interface ISMESetRepository : IRepository<SMESet, int>
{
    Task ClearDB();
}
