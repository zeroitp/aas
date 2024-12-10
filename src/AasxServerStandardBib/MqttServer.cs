﻿using MQTTnet;
using MQTTnet.Server;
using System.Threading.Tasks;

/* For Mqtt Content:

MIT License

MQTTnet Copyright (c) 2016-2019 Christian Kratky
*/

namespace AasxMqttServer
{
    class MqttServer
    {
        IMqttServer mqttServer;

        public MqttServer()
        {
            mqttServer = new MqttFactory().CreateMqttServer();
        }

        public async Task MqttSeverStartAsync()
        {
            //Start a MQTT server.
            await mqttServer.StartAsync(new MqttServerOptionsBuilder()
                .Build());
        }

        public async Task MqttSeverStopAsync()
        {
            await mqttServer.StopAsync();
        }
    }
}
