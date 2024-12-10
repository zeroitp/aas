namespace AasxServerDB.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;
using AasxServerDB.Entities;
using AHI.Infrastructure.Repository.Generic;
using Microsoft.EntityFrameworkCore;

public class AASSetRepository : GenericRepository<AASSet, int>, IAASSetRepository
{
    private readonly AasContext _context;
    public AASSetRepository(AasContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<AASSet> AsFetchable()
    {
        return _context.AASSets.AsNoTracking().Where(x => !x.IsDeleted);
    }

    public IQueryable<AASSet> AsQueryable()
    {
        return base.AsQueryable().Where(x => !x.IsDeleted);
    }

    public Task<AASSet> FindAsync(int id)
    {
        return AsQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public async Task<bool> RemoveAsync(int id)
    {
        var aas = await FindAsync(id);
        if (aas != null)
        {
            aas.IsDeleted = true;
            await UpdateAsync(id, aas);
        }

        return false;
    }

    public async Task ClearDB()
    {
        await _context.AASSets.ExecuteDeleteAsync();
    }

    protected override void Update(AASSet requestObject, AASSet targetObject)
    {
        targetObject.Name = requestObject.Name;
        targetObject.TimeStamp = DateTime.UtcNow;
        targetObject.ResourcePath = requestObject.ResourcePath;
        targetObject.Parent = requestObject.Parent;
    }
}
