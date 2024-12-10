namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Org.BouncyCastle.Bcpg.Sig;

public class ExportAssetTemplate : ExportFile
{
}

public class ExportFile
{
    public string ObjectType { get; set; }
    public IEnumerable<string> Ids { get; set; }
}