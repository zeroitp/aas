namespace AasxServerDB.Repositories;

using AHI.Infrastructure.Repository;

public class AASUnitOfWork : BaseUnitOfWork, IAASUnitOfWork
{
    public IAASSetRepository AASSets { get; private set; }

    public ISMSetRepository SMSets { get; private set; }

    public ISMESetRepository SMESets { get; private set; }

    //public ISValueSetRepository SValueSets { get; private set; }

    //public IIValueSetRepository IValueSets { get; private set; }

    //public IDValueSetRepository DValueSets { get; private set; }

    //public IOValueSetRepository OValueSets { get; private set; }

    public AASUnitOfWork(AasContext context,
        IAASSetRepository aasSets,
        ISMSetRepository smSets,
        ISMESetRepository smeSets
        //ISValueSetRepository svalueSets,
        //IIValueSetRepository ivalueSets,
        //IDValueSetRepository dvalueSets,
        //IOValueSetRepository ovalueSets
        ) : base(context)
    {
        AASSets = aasSets;
        SMSets = smSets;
        SMESets = smeSets;
        //SValueSets = svalueSets;
        //IValueSets = ivalueSets;
        //DValueSets = dvalueSets;
        //OValueSets = ovalueSets;
    }
}
