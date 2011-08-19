using System.Collections.Generic;
using System.Diagnostics;

namespace NServiceBus.Hosting
{
    internal class DynamicEndpointStarter
    {
        public DynamicEndpointStarter()
        {
        }

        public void Start(IEnumerable<ServiceToRun> toHost)
        {
            foreach(var service in toHost)
            {
                var processStartInfo = new ProcessStartInfo(service.EntryPoint,
                                                            //"/install " +
                                                            "/serviceName:\"" + service.ServiceName +
                                                            "\" /displayName:\"" + service.ServiceName +
                                                            "\" /description:\"" + service.ServiceName + "\"");
                Process.Start(processStartInfo);

            }
           
        }
    }
}