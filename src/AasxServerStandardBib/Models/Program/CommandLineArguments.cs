namespace AasxServerStandardBib.Models.Program;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CommandLineArguments
{
    // ReSharper disable UnusedAutoPropertyAccessor.Local
#pragma warning disable 8618
    public string Host { get; set; }
    public string Port { get; set; }
    public bool Https { get; set; }
    public string DataPath { get; set; }
    public bool Rest { get; set; }
    public bool Opc { get; set; }
    public bool Mqtt { get; set; }
    public bool DebugWait { get; set; }
    public int? OpcClientRate { get; set; }
    public string[] Connect { get; set; }
    public string ProxyFile { get; set; }
    public bool NoSecurity { get; set; }
    public bool Edit { get; set; }
    public string Name { get; set; }
    public string ExternalRest { get; set; }
    public string ExternalBlazor { get; set; }
    public bool ReadTemp { get; set; }
    public int SaveTemp { get; set; }
    public string SecretStringAPI { get; set; }
    public string Tag { get; set; }
    public bool HtmlId { get; set; }
    public int AasxInMemory { get; set; }
    public bool WithDb { get; set; }
    public bool NoDbFiles { get; set; }
    public int StartIndex { get; set; }
#pragma warning restore 8618
    // ReSharper enable UnusedAutoPropertyAccessor.Local
}