namespace AasxServerStandardBib.Models.Program;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class NewDataAvailableArgs : EventArgs
{
    public int signalNewDataMode;

    public NewDataAvailableArgs(int mode = 2)
    {
        signalNewDataMode = mode;
    }
}