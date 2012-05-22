using System.Collections.Generic;

namespace NServiceBus.Licensing
{
    /// <summary>
    /// NServiceBus License information
    /// </summary>
    public class License
    {
        public string LicenseType { get; set; }
        public IDictionary<string, string> LicenseAttributes { get; set; }
    }
}
