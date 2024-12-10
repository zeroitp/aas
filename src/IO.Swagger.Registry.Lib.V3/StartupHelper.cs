namespace IO.Swagger.Lib.V3;

using AasxServerDB;
using AasxServerDB.Repositories;
using AasxServerDB.Repositories.AHIRepo;
using AasxServerStandardBib.EventHandlers.Abstracts;
using AasxServerStandardBib.Services;
using AHI.Infrastructure.MultiTenancy.Extension;
using I.Swagger.Registry.Lib.V3.Services;
using IO.Swagger.Controllers;
using IO.Swagger.Lib.V3.EventHandlers;
using IO.Swagger.Lib.V3.Services;
using IO.Swagger.Registry.Lib.V3.EventHandlers;
using IO.Swagger.Registry.Lib.V3.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class StartupHelper
{
    public static void AddAhiServices(IServiceCollection services)
    {
        services.AddMultiTenantService();
        services.AddScoped<TenantProvider>();

        services.AddDbContext<AasContext>((service, option) =>
        {
            var tenantProvider = service.GetRequiredService<TenantProvider>();
            var connectionString = string.Empty;
            if (!string.IsNullOrEmpty(tenantProvider.ProjectId))
            {
                connectionString = tenantProvider.GetAASConnectionString();
            }
            else
            {
                var configuration = service.GetRequiredService(typeof(IConfiguration)) as IConfiguration;
                connectionString = configuration["DatabaseConnection:ConnectionString__NoTenant"];
            }

            option.UseNpgsql(connectionString);

        });

        services.AddDbContext<AHIContext>((service, option) =>
        {
            var tenantProvider = service.GetRequiredService<TenantProvider>();
            var connectionString = string.Empty;
            if (!string.IsNullOrEmpty(tenantProvider.ProjectId))
            {
                connectionString = tenantProvider.GetAHIConnectionString();
            }
            else
            {
                var configuration = service.GetRequiredService(typeof(IConfiguration)) as IConfiguration;
                connectionString = configuration["DatabaseConnection:ConnectionString__AHI__NoTenant"];
            }

            option.UseNpgsql(connectionString);
        });

        services
            .AddScoped<IAASSetRepository, AASSetRepository>()
            .AddScoped<ISMSetRepository, SMSetRepository>()
            .AddScoped<ISMESetRepository, SMESetRepository>()
            //.AddScoped<ISValueSetRepository, SValueSetRepository>()
            //.AddScoped<IIValueSetRepository, IValueSetRepository>()
            //.AddScoped<IDValueSetRepository, DValueSetRepository>()
            //.AddScoped<IOValueSetRepository, OValueSetRepository>()
            .AddScoped<IAASUnitOfWork, AASUnitOfWork>()

            .AddScoped<IDeviceMetricSnapshotRepository, DeviceMetricSnapshotRepository>()
            .AddScoped<IAHIUnitOfWork, AHIUnitOfWork>()

            .AddScoped<RuntimeAssetAttributeHandler>()
            .AddSingleton<MqttClientManager>()
            .AddScoped<TimeSeriesService>()
            .AddScoped<AasApiHelperService>()
            .AddSingleton<NotificationService>()

            .AddSingleton<EventPublisher>()
            .AddSingleton<IEventHandler, CalculateRuntimeAttributeHandler>()
            .AddSingleton<IEventHandler, RuntimeAttributeCreationHandler>()
            .AddSingleton<IEventHandler, NotifyAssetChangedHandler>()
            .AddSingleton<IEventHandler, AliasAttributeHandler>()
            .AddSingleton<IEventHandler, TemplateUpdateHandler>()

            .AddScoped<AssetAdministrationShellRepositoryAPIApiController>()
            .AddScoped<SubmodelRepositoryAPIApiController>();
    }
}