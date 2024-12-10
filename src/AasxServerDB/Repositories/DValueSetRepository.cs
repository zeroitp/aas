//namespace AasxServerDB.Repositories;
//using AasxServerDB.Entities;
//using AHI.Infrastructure.Repository.Generic;
//using Microsoft.EntityFrameworkCore;

//public class DValueSetRepository : GenericRepository<DValueSet, int>, IDValueSetRepository
//{
//    private readonly AasContext _context;
//    public DValueSetRepository(AasContext context) : base(context)
//    {
//        _context = context;
//    }

//    public async Task ClearDB()
//    {
//        await _context.DValueSets.ExecuteDeleteAsync();
//    }

//    protected override void Update(DValueSet requestObject, DValueSet targetObject)
//    {
//        targetObject.Value = requestObject.Value;
//        targetObject.Annotation = requestObject.Annotation;
//    }
//}
