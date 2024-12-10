namespace AasxServerStandardBib.Models.Program;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class aasDirectoryParameters
{
    public string source;
    public List<aasListParameters> aasList;

    public aasDirectoryParameters()
    {
        aasList = new List<aasListParameters> { };
    }
}