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
using IO.Swagger.Registry.Lib.V3.Models;

public class TemplateUpdateHandler(
    MqttClientManager mqttClientManager,
    IServiceProvider serviceProvider,
    ILogger<TemplateUpdateHandler> logger) : IEventHandler
{
    public async Task Start()
    {
        var subscriber = await mqttClientManager.GetSubscriber(nameof(TemplateUpdateHandler));

        var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(AasEvents.TemplateElementUpdated, MqttQualityOfServiceLevel.ExactlyOnce)
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
        var updatedItem = JsonConvert.DeserializeObject<TemplateAttributeUpdatedMessage>(json);

        if (updatedItem != null)
        {
            using var scope = serviceProvider.CreateScope();
            var aasRegistryService = scope.ServiceProvider.GetService<IAasRegistryService>();

            switch (updatedItem.Type)
            {
                case AttributeUpdatedType.Add:
                    await aasRegistryService.AddNewElementFromTemplate(updatedItem.AASIdShort, updatedItem.AttributeIdShort);
                    break;
                case AttributeUpdatedType.Edit:
                    await aasRegistryService.UpdateElementAsTemplateChange(updatedItem.AASIdShort, updatedItem.AttributeIdShort);
                    break;
                case AttributeUpdatedType.Remove:
                    await aasRegistryService.RemoveElementAsTempalateChange(updatedItem.AASIdShort, updatedItem.AttributeIdShort);
                    break;
                default:
                    break;
            }
        }
    }
}