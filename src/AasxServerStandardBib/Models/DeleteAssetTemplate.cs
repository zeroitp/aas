namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DeleteAssetTemplate
{
    public Guid Id { set; get; }
    public IEnumerable<Guid> Ids { get; set; } = new List<Guid>();
}
