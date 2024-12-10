namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ImportFile
{
    public string ObjectType { get; set; }
    public IEnumerable<string> FileNames { get; set; }
}
