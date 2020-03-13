using System;
using System.Threading;

namespace IntegrationTests
{
    public static class MqttTestHelper
    {
        static int _i;
        public static string GetClientId()
        {
            return string.Concat( "Client", _i++ );
        }
    }
}
