namespace AasxServerStandardBib.Services;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

public class EventPublisher(MqttClientManager mqttClientManager)
{
    public async Task Publish(string topic, object payload)
    {
        var publisher = await mqttClientManager.GetPublisher();

        var jsonOptions = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(JsonSerializer.SerializeToUtf8Bytes(payload, jsonOptions))
            .WithExactlyOnceQoS()
            .Build();

        _ = await publisher.PublishAsync(message);
    }
}