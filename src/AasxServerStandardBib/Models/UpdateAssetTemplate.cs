namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.JsonPatch;

public class UpdateAssetTemplate
{
    public Guid? Id { get; set; }
    public string Name { get; set; }
    public string? CurrentUserUpn { set; get; }
    public DateTime? CurrentTimestamp { set; get; }
    public List<Microsoft.AspNetCore.JsonPatch.Operations.Operation> Attributes { set; get; } 
}