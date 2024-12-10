namespace AasxServerDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BaseEntity
{
    public bool IsDeleted { get; set; }
    public string? TemplateId { get; set; }
}

public class BaseAttribute : BaseEntity
{
    public bool IsOverridden { get; set; }
}
