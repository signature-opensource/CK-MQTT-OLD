using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace CK.MQTT
{
    public static class Properties
    {
        public static ResourceManager Resources
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                Debug.Assert( assembly.GetManifestResourceNames().Contains( "CK.MQTT.Client.Abstractions.Properties.Resources.resources" ));
                return new ResourceManager( "CK.MQTT.Client.Abstractions.Properties.Resources", assembly );
            }
        }
    }
}
