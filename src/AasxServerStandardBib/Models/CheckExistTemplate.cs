namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;

public class CheckExistTemplate
{
    public IEnumerable<Guid> Ids { get; set; }

    public CheckExistTemplate(IEnumerable<Guid> ids)
    {
        Ids = ids;
    }
}