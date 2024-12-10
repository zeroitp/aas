namespace AasxServerDB.Repositories;
using System.Threading.Tasks;
using AasxServerDB.Entities;
using AHI.Infrastructure.Repository.Generic;

public interface ISMSetRepository : IRepository<SMSet, int>
{
    Task ClearDB();
}
