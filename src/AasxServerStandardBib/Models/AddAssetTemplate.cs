namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

public class AddAssetTemplate
{
    public string Name { get; set; }
    public IEnumerable<AssetTemplateAttribute> Attributes { get; set; } = new List<AssetTemplateAttribute>();
}
