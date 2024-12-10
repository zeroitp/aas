namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Org.BouncyCastle.Utilities;

public class GetTemplateByCriteria : BaseCriteria
{
    //public bool ClientOverride { get; set; } = false;
    public GetTemplateByCriteria()
    {
        Sorts = DefaultSearchConstants.DEFAULT_SORT;
    }
}

public class GetValidTemplatesByCriteria : BaseCriteria
{
    //public bool ClientOverride { get; set; } = false;
    public GetValidTemplatesByCriteria()
    {
        Sorts = DefaultSearchConstants.DEFAULT_SORT;
    }
}

public class GetTemplateKeyTypeByCriteria : BaseCriteria
{
    //public bool ClientOverride { get; set; } = false;
}