//namespace AasxServerDB.Repositories;
//using AasxServerDB.Entities;
//using AHI.Infrastructure.Repository.Generic;
//using Microsoft.EntityFrameworkCore;

//public class SValueSetRepository : GenericRepository<SValueSet, int>, ISValueSetRepository
//{
//    private readonly AasContext _context;
//    public SValueSetRepository(AasContext context) : base(context)
//    {
//        _context = context;
//    }

//    protected override void Update(SValueSet requestObject, SValueSet targetObject)
//    {
//        targetObject.Value = requestObject.Value;
//        targetObject.Annotation = requestObject.Annotation;
//    }

//    public async Task ClearDB()
//    {
//        await _context.SValueSets.ExecuteDeleteAsync();
//    }
//}
