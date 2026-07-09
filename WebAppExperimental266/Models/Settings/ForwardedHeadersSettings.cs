using System.Net;

namespace WebAppExperimental266.Models.Settings
{
    public class ForwardedHeadersSettings
    {
        public bool EnableForwardedHeaders { get; set; } = false;

        public int ForwardLimit { get; set; } = 1;

        public List<string> KnownProxies { get; set; } = new();

        public List<string> KnownNetworks { get; set; } = new();
    }
}
