namespace AasxServerDB.Repositories;
using AasxServerDB.Entities;
using AHI.Infrastructure.Repository.Generic;
using Microsoft.EntityFrameworkCore;

public class SMESetRepository : GenericRepository<SMESet, int>, ISMESetRepository
{
    private readonly AasContext _context;
    public SMESetRepository(AasContext context) : base(context)
    {
        _context = context;
    }

    protected override void Update(SMESet requestObject, SMESet targetObject)
    {
        targetObject.SMEType = requestObject.SMEType;
        targetObject.TValue = requestObject.TValue;
        targetObject.SemanticId = requestObject.SemanticId;
        targetObject.TimeStamp = requestObject.TimeStamp;
        targetObject.TimeStampTree = requestObject.TimeStampTree;
        targetObject.RawJson = requestObject.RawJson;
        targetObject.Name = requestObject.Name;
        targetObject.MetaData = requestObject.MetaData;
    }

    public async Task<bool> RemoveAsync(int id)
    {
        var aas = await AsQueryable().Where(x => x.Id == id && !x.IsDeleted).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        if (aas != null)
        {
            aas.IsDeleted = true;
            await UpdateAsync(id, aas);
        }

        return false;
    }

    public IQueryable<SMESet> AsFetchable()
    {
        return _context.SMESets.AsNoTracking().Where(x => !x.IsDeleted);
    }

    public IQueryable<SMESet> AsQueryable()
    {
        return base.AsQueryable().Where(x => !x.IsDeleted);
    }

    public Task<SMESet> FindAsync(int id)
    {
        return AsFetchable().Where(x => x.Id == id && !x.IsDeleted).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
    }

    public async Task ClearDB()
    {
        await _context.SMESets.ExecuteDeleteAsync();
    }
}
