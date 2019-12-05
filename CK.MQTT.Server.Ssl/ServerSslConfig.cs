using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CK.MQTT.Ssl
{
    public class ServerSslConfig
    {
        public SslProtocols SslProtocols { get; set; }
        public X509Certificate2 ServerCertificate { get; set; }
    }
}
