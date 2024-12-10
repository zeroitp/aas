namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SendConfigurationToDeviceIot
{
    public object Value { get; set; }
    public Guid RowVersion { get; set; }
    public string DeviceId { get; set; }
    public string MetricKey { get; set; }
    public Guid AssetId { get; set; }
    public Guid AttributeId { get; set; }
}

public class SendConfigurationResultDto
{
    public bool IsSuccess { get; set; }
    public System.Guid NewRowVersion { get; set; }

    public SendConfigurationResultDto(bool isSuccess, System.Guid newRowVersion)
    {
        IsSuccess = isSuccess;
        NewRowVersion = newRowVersion;
    }
}
