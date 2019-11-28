﻿using System.Net.Sockets;
using CK.MQTT;
using CK.MQTT.Sdk;

namespace ConsoleApp1.Server
{
	internal class TcpChannel
	{
		private TcpClient client;
		private PacketBuffer packetBuffer;
		private MqttConfiguration configuration;

		public TcpChannel(TcpClient client, PacketBuffer packetBuffer, MqttConfiguration configuration)
		{
			this.client = client;
			this.packetBuffer = packetBuffer;
			this.configuration = configuration;
		}
	}
}