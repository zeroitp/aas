namespace AasxServerStandardBib.Models.Program;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum ExitCode : int
{
    Ok = 0,
    ErrorServerNotStarted = 0x80,
    ErrorServerRunning = 0x81,
    ErrorServerException = 0x82,
    ErrorInvalidCommandLine = 0x100
};