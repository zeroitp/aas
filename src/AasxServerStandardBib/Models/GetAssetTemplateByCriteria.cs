namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Org.BouncyCastle.Utilities;

public class GetAssetTemplateByCriteria : BaseCriteria
{
    public GetAssetTemplateByCriteria()
    {
        Sorts = DefaultSearchConstants.DEFAULT_SORT;
    }
}
