//namespace AasxServerDB.Repositories;
//using AasxServerDB.Entities;
//using AHI.Infrastructure.Repository.Generic;
//using Microsoft.EntityFrameworkCore;

//public class IValueSetRepository : GenericRepository<IValueSet, int>, IIValueSetRepository
//{
//    private readonly AasContext _context;
//    public IValueSetRepository(AasContext context) : base(context)
//    {
//        _context = context;
//    }

//    protected override void Update(IValueSet requestObject, IValueSet targetObject)
//    {
//        targetObject.Value = requestObject.Value;
//        targetObject.Annotation = requestObject.Annotation;
//    }


//    public async Task ClearDB()
//    {
//        await _context.IValueSets.ExecuteDeleteAsync();
//    }
//}
