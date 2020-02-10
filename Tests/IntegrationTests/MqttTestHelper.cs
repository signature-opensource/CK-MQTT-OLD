using System;

namespace IntegrationTests
{
    public static class MqttTestHelper
    {
        public static string GetClientId()
        {
            return string.Concat( "Client", Guid.NewGuid().ToString().Replace( "-", string.Empty ).Substring( 0, 15 ) );
        }
    }
}
