using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;

namespace CK.MQTT
{
    public static class Properties
    {
        public static ResourceManager Resources
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                Debug.Assert( assembly.GetManifestResourceNames().Contains( "CK.MQTT.Client.Properties.Resources.resources" ));
                return new ResourceManager( "CK.MQTT.Client.Properties.Resources", assembly );
            }
        }
    }
}
