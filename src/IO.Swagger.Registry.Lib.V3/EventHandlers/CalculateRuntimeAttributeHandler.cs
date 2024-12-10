namespace IO.Swagger.Registry.Lib.V3.EventHandlers;

using AasxServerStandardBib;
using System;
using System.Threading.Tasks;
using AasxServerStandardBib.Services;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Client;
using MQTTnet.Client.Subscribing;
using MQTTnet.Protocol;
using AasxServerStandardBib.EventHandlers.Abstracts;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using AasxServerStandardBib.Models;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using IO.Swagger.Models;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;
using IO.Swagger.Registry.Lib.V3.Services;
using IO.Swagger.Registry.Lib.V3.Interfaces;
using AasxServerDB.Helpers;
using I.Swagger.Registry.Lib.V3.Services;

public class CalculateRuntimeAttributeHandler(
    MqttClientManager mqttClientManager,
    IServiceProvider serviceProvider,
    EventPublisher eventPublisher,
    ILogger<CalculateRuntimeAttributeHandler> logger) : IEventHandler
{
    public async Task Start()
    {
        var subscriber = await mqttClientManager.GetSubscriber(nameof(CalculateRuntimeAttributeHandler));

        var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(AasEvents.SubmodelElementUpdated, MqttQualityOfServiceLevel.ExactlyOnce)
            .Build();

        _ = await subscriber.SubscribeAsync(options, cancellationToken: default);

        _ = subscriber.UseApplicationMessageReceivedHandler(async (e) =>
        {
            try
            {
                await Handle(e);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        });
    }

    private async Task Handle(MQTTnet.MqttApplicationMessageReceivedEventArgs eArgs)
    {
        var json = Encoding.UTF8.GetString(eArgs.ApplicationMessage.Payload);
        var jsonNode = JObject.Parse(json);
        var aasId = (jsonNode.Property("aasId").Value as JValue).Value<string>();
        var attribute = (jsonNode.Property("attribute").Value as JObject).Value<JObject>();
        var idShort = attribute["IdShort"]?.Value<string>();
        using var scope = serviceProvider.CreateScope();
        var aasApiHelper = scope.ServiceProvider.GetService<AasApiHelperService>();
        var timeSeriesService = scope.ServiceProvider.GetService<TimeSeriesService>();
        var aasRegistryService = scope.ServiceProvider.GetService<IAasRegistryService>();

        var (aas, submodelElements) = await aasRegistryService.GetFullAasByIdAsync(Guid.Parse(aasId));

        if (aas != null)
        {
            foreach (var sme in submodelElements)
            {
                if (sme is SubmodelElementCollection smc && smc.Category == AttributeTypeConstants.TYPE_RUNTIME)
                {
                    var triggerAttributeIdsJson = smc.GetExtensionValue("TriggerAttributeIds");
                    var triggerAttributeIds = triggerAttributeIdsJson != null ? JsonConvert.DeserializeObject<IEnumerable<Guid>>(triggerAttributeIdsJson) : null;
                    if (triggerAttributeIds?.Contains(Guid.Parse(idShort)) != true)
                        continue;

                    var dictionary = new Dictionary<string, object>();
                    foreach (var usedAttributeId in triggerAttributeIds)
                    {
                        var usedSme = await aasRegistryService.GetSubmodelElementByPathSubmodelRepo(aas.IdShort, usedAttributeId.ToString(), level: LevelEnum.Deep, extent: ExtentEnum.WithoutBlobValue);
                        var dto = await aasApiHelper.ToAttributeDto(usedSme as ISubmodelElement, aasId);
                        dictionary[dto.AttributeId.ToString()] = dto.Series.FirstOrDefault()?.v;
                    }

                    var expressionCompile = smc.GetExtensionValue("ExpressionCompile");

                    var runtimeValue = await GetRuntimeValue(expressionCompile, dictionary);

                    var series = TimeSeriesHelper.BuildSeriesDto(value: runtimeValue);
                    //await aasRegistryService.UpdateSnapshot(Guid.Parse(aasId), Guid.Parse(sme.IdShort), series);
                    smc.UpdateSnapshot(series);

                    var attributeId = Guid.Parse(smc.IdShort);
                    await timeSeriesService.AddRuntimeSeries(Guid.Parse(aasId), attributeId, series);

                    await aasRegistryService.ReplaceSubmodelElementByPath(aas.IdShort, attributeId.ToString(), smc);

                    await eventPublisher.Publish(AasEvents.SubmodelElementUpdated, new { attribute = sme, aasId });
                    await eventPublisher.Publish(AasEvents.AasUpdated, aas.IdShort);
                }
            }
        }
    }

    private static async Task<object?> GetRuntimeValue(string expressionCompile, Dictionary<string, object> dictionary)
    {
        var scriptOptions = ScriptOptions.Default;

        // Add reference to mscorlib
        var mscorlib = typeof(object).GetTypeInfo().Assembly;
        var systemCore = typeof(Enumerable).GetTypeInfo().Assembly;

        var references = new[] { mscorlib, systemCore };
        scriptOptions = scriptOptions.AddReferences(references);
        scriptOptions = scriptOptions.AddImports("System");

        return await CSharpScript.EvaluateAsync(expressionCompile, options: scriptOptions, globals: new RuntimeExpressionGlobals { request = dictionary });
    }
}

public class RuntimeAttributeCreationHandler(
    MqttClientManager mqttClientManager,
    IServiceProvider serviceProvider,
    EventPublisher eventPublisher,
    ILogger<RuntimeAttributeCreationHandler> logger) : IEventHandler
{
    public async Task Start()
    {
        var subscriber = await mqttClientManager.GetSubscriber(nameof(RuntimeAttributeCreationHandler));

        var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(AasEvents.RuntimeElementUpdated, MqttQualityOfServiceLevel.ExactlyOnce)
            .Build();

        _ = await subscriber.SubscribeAsync(options, cancellationToken: default);

        _ = subscriber.UseApplicationMessageReceivedHandler(async (e) =>
        {
            try
            {
                await Handle(e);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        });
    }

    private async Task Handle(MQTTnet.MqttApplicationMessageReceivedEventArgs eArgs)
    {
        var json = Encoding.UTF8.GetString(eArgs.ApplicationMessage.Payload);
        var jsonNode = JObject.Parse(json);
        var aasId = (jsonNode.Property("aasId").Value as JValue).Value<string>();
        var attribute = (jsonNode.Property("attribute").Value as JObject).Value<JObject>();
        var idShort = attribute["IdShort"]?.Value<string>();
        using var scope = serviceProvider.CreateScope();
        var aasRegistryService = scope.ServiceProvider.GetService<IAasRegistryService>();

        var category = attribute["Category"]?.Value<string>();

        if (!string.IsNullOrEmpty(category) && category.Equals(AttributeTypeConstants.TYPE_RUNTIME, StringComparison.OrdinalIgnoreCase))
        {
            var (sm, sme) = await aasRegistryService.FindSmeByGuid(Guid.Parse(idShort));
            if (sme is SubmodelElementCollection smc && smc.Category == AttributeTypeConstants.TYPE_RUNTIME)
            {
                var triggerAttributeIdsJson = smc.GetExtensionValue("TriggerAttributeIds");
                var triggerAttributeIds = triggerAttributeIdsJson != null ? JsonConvert.DeserializeObject<IEnumerable<Guid>>(triggerAttributeIdsJson) : null;

                if (triggerAttributeIds != null && triggerAttributeIds.Any())
                {
                    idShort = triggerAttributeIds.First().ToString();

                    (sm, sme) = await aasRegistryService.FindSmeByGuid(Guid.Parse(idShort));

                    await eventPublisher.Publish(AasEvents.SubmodelElementUpdated, new { attribute = sme, aasId });
                }
            }
        }
    }
}