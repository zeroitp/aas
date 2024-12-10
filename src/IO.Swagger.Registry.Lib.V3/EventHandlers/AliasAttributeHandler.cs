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
using Microsoft.Extensions.Logging;
using IO.Swagger.Registry.Lib.V3.Services;
using IO.Swagger.Registry.Lib.V3.Interfaces;
using Newtonsoft.Json.Linq;

public class AliasAttributeHandler(
    MqttClientManager mqttClientManager,
    IServiceProvider serviceProvider,
    ILogger<AliasAttributeHandler> logger) : IEventHandler
{
    public async Task Start()
    {
        var subscriber = await mqttClientManager.GetSubscriber(nameof(AliasAttributeHandler));

        var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(AasEvents.AliasElementUpdated, MqttQualityOfServiceLevel.ExactlyOnce)
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
        var aasRegistryService = scope.ServiceProvider.GetService<IAasRegistryService>();

        var relatedSmes = await aasRegistryService.GetAliasRelatedSME(idShort);
        if (relatedSmes != null && relatedSmes.Count > 0)
        {
            foreach (var relatedSme in relatedSmes)
            {
                var relatedAlias = await aasRegistryService.GetSubmodelElementByPath(relatedSme.SMSet.IdShort, relatedSme.IdShort);
                var (rootAas, rootSmId, rootSme, aliasPath) = await aasApiHelper.GetRootAliasSme(relatedAlias as IReferenceElement);

                relatedAlias.Extensions =
                                [
                                    new Extension(
                                        name: "RootAasIdShort",
                                        valueType: DataTypeDefXsd.String,
                                        value: rootAas.IdShort?.ToString()),
                                    new Extension(
                                        name: "RootSmId",
                                        valueType: DataTypeDefXsd.String,
                                        value: rootSmId),
                                    new Extension(
                                        name: "RootSmeIdShort",
                                        valueType: DataTypeDefXsd.String,
                                        value: rootSme.IdShort?.ToString()),
                                    new Extension(
                                        name: "AliasPath",
                                        valueType: DataTypeDefXsd.String,
                                        value: aliasPath?.ToString())
                                ];

                await aasRegistryService.UpdateAlias(rootSmId, idShort, relatedAlias as IReferenceElement, aliasPath);
            }
        }
    }
}