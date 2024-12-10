namespace AasxServerStandardBib.Models.Program;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class TransmitData
{
    public string source;
    public string destination;
    public string type;
    public string encrypt;
    public string extensions;
    public List<string> publish;

    public TransmitData()
    {
        publish = new List<string> { };
    }
}

public class TransmitFrame
{
    public string source;
    public List<TransmitData> data;

    public TransmitFrame()
    {
        data = new List<TransmitData> { };
    }
}