using System;
using System.Collections.Generic;
using System.Text;

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
