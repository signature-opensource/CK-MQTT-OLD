using System;
using System.Collections.Generic;
using CK.MQTT.Sdk;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Server
{
	interface IListener
	{
		void Start();

		void Stop();

		Task<T> AcceptClientAsync<T>() where T : IMqttChannel<byte[]>;
	}
}
