namespace IO.Swagger.Registry.Lib.V3.Models;

using System;
using AHI.Infrastructure.Audit.Model;

public class AssetNotificationMessage : NotificationMessage
{
    public Guid AssetId { get; set; }

    public AssetNotificationMessage(Guid assetId, string type, object payload) : base(type, payload)
    {
        AssetId = assetId;
    }
}