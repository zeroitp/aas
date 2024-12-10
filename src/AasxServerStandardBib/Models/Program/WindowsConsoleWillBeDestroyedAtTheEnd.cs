namespace AasxServerStandardBib.Models.Program;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Checks whether the console will persist after the program exits.
/// This should run only on Windows as it depends on kernel32.dll.
///
/// The code has been adapted from: https://stackoverflow.com/a/63135555/1600678
/// </summary>
public static class WindowsConsoleWillBeDestroyedAtTheEnd
{
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern uint GetConsoleProcessList(uint[] processList, uint processCount);

    public static bool Check()
    {
        var processList = new uint[1];
        var processCount = GetConsoleProcessList(processList, 1);

        return processCount == 1;
    }
}
