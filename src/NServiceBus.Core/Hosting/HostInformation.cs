namespace NServiceBus.Hosting
{
    using System.Collections.Generic;

    public static class HostInformation
    {
        public static string HostId { get; set; }

        public static Dictionary<string, string> Properties { get; set; }

        //new Dictionary<string, string>
        //    {
        //        {"WinService","Sales-1.0.0"},
        //        {"Machine","server2045"},
        //        {"IISSite","acme.com"},
        //        {"RoleInstanceId","2"},
        //        {"PID","1045"},
        //        {"Path","C:/somepath"}
        //    }
    }
}