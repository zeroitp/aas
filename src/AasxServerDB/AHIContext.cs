namespace AasxServerDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public class AHIContext : DbContext
{
    public DbSet<DeviceMetricTimeseries> DeviceMetrics { get; set; }

    //protected override void OnConfiguring(DbContextOptionsBuilder options)
    //{
    //    if (_con == null)
    //        throw new Exception("No Configuration!");

    //    var connectionString = _con["DatabaseConnection:ConnectionStrings__AHI"];
    //    if (connectionString.IsNullOrEmpty())
    //        throw new Exception("No connectionString in appsettings");

    //    if (connectionString != null && connectionString.Contains("$DATAPATH"))
    //        connectionString = connectionString.Replace("$DATAPATH", _dataPath);

    //    //device_34e5ee62429c4724b3d03891bd0a08c9
    //    connectionString = connectionString.Replace("{{projectid}}", ProjectId);

    //    if (connectionString != null && connectionString.ToLower().Contains("host")) // PostgreSQL
    //    {
    //        IsPostgres = true;
    //        options.UseNpgsql(connectionString);
    //    }
    //    else // SQLite
    //    {
    //        IsPostgres = false;
    //        options.UseSqlite(connectionString);
    //    }
    //}

    public AHIContext() { }

    public AHIContext(DbContextOptions<AHIContext> options) : base(options)
    {
    }

    protected AHIContext(DbContextOptions options) : base(options)
    {
    }
}

[Keyless]
public class DeviceMetricTimeseries
{
    public string DeviceId { get; set; }
    public string MetricKey { get; set; }
    public double? Value { get; set; }
    public DateTime TS { get; set; }
}