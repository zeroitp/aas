namespace AasxServerDB.Repositories;
using AasxServerDB.Entities;
using AHI.Infrastructure.Repository.Generic;
using Microsoft.EntityFrameworkCore;

public class SMSetRepository : GenericRepository<SMSet, int>, ISMSetRepository
{
    private readonly AasContext _context;
    public SMSetRepository(AasContext context) : base(context)
    {
        _context = context;
    }

    protected override void Update(SMSet requestObject, SMSet targetObject)
    {
        targetObject.SemanticId = requestObject.SemanticId;
        targetObject.TimeStamp = requestObject.TimeStamp;
        targetObject.TimeStampTree = requestObject.TimeStampTree;
        targetObject.Submodel = requestObject.Submodel;
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
        await _context.SMSets.ExecuteDeleteAsync();
    }
}
