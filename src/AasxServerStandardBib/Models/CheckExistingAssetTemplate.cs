namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;

public class CheckExistingAssetTemplate
{
    public IEnumerable<Guid> Ids { get; set; }

    public CheckExistingAssetTemplate(IEnumerable<Guid> ids)
    {
        Ids = ids;
    }
}