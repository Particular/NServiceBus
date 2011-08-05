namespace NServiceBus.Gateway.Channels
{
    using System.Collections.Generic;
    using System.Configuration;

    public class ChannelTypes
    {
        private static readonly Dictionary<string,string> SchemeToChannelType = new Dictionary<string, string>();
        static ChannelTypes()
        {
            // http(s) => Channel Type "Http"
            SchemeToChannelType.Add("http", "Http");
            SchemeToChannelType.Add("https", "Http");
        }

        public static void RegisterChannelType( string scheme, string channelType )
        {
            SchemeToChannelType.Add(scheme, channelType);
        }

        public static string LookupByUrl( string url )
        {
            var parts = url.Split(':');
            if(parts.Length >= 2)
            {
                return LookupByScheme(parts[0]);
            } else
                throw new ConfigurationErrorsException("Improperly formatted URL found: '" + url + "'");
        }

        public static string LookupByScheme(string scheme)
        {
            // By default scheme => channel type of same name
            string channelType = scheme;
            if ( SchemeToChannelType.ContainsKey(scheme) )
            {
                channelType = SchemeToChannelType[scheme];
            }
            return channelType;
        }
    }
}
