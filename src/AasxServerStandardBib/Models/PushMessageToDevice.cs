namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;

public class PushMessageToDevice
{
    public string Id { get; set; }
    public IEnumerable<CloudToDeviceMessage> Metrics { get; set; }
    public PushMessageToDevice(string id, IEnumerable<CloudToDeviceMessage> metrics)
    {
        Id = id;
        Metrics = metrics;
    }
}
public class CloudToDeviceMessage
{
    public string Key { get; set; }
    public string Value { get; set; }
    public string DataType { get; set; }
}