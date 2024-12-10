namespace AasxServerStandardBib.Models.Program;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua.Configuration;

public class ApplicationMessageDlg : IApplicationMessageDlg
{
    private string message = string.Empty;
    private bool ask = false;

    public override void Message(string text, bool ask)
    {
        this.message = text;
        this.ask = ask;
    }

    public override async Task<bool> ShowAsync()
    {
        if (ask)
        {
            message += " (y/n, default y): ";
            Console.Write(message);
        }
        else
        {
            Console.WriteLine(message);
        }

        if (ask)
        {
            try
            {
                ConsoleKeyInfo result = Console.ReadKey();
                Console.WriteLine();
                return await Task.FromResult((result.KeyChar == 'y') || (result.KeyChar == 'Y') || (result.KeyChar == '\r'));
            }
            catch
            {
                // intentionally fall through
            }
        }

        return await Task.FromResult(true);
    }
}