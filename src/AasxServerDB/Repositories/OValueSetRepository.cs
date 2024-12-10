//namespace AasxServerDB.Repositories;
//using AasxServerDB.Entities;
//using AHI.Infrastructure.Repository.Generic;
//using Microsoft.EntityFrameworkCore;

//public class OValueSetRepository : GenericRepository<OValueSet, int>, IOValueSetRepository
//{
//    private readonly AasContext _context;
//    public OValueSetRepository(AasContext context) : base(context)
//    {
//        _context = context;
//    }

//    protected override void Update(OValueSet requestObject, OValueSet targetObject)
//    {
//        targetObject.Value = requestObject.Value;
//        targetObject.Attribute = requestObject.Attribute;
//    }

//    public async Task ClearDB()
//    {
//        await _context.OValueSets.ExecuteDeleteAsync();
//    }
//}
