using System;
using System.Diagnostics;
using NServiceBus.Hosting.Profiles;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    /// <summary>
    /// Handles the PerformanceCounters profile.
    /// </summary>
    public class PerformanceCountersProfileHandler : IHandleProfile<PerformanceCounters>
    {
        void IHandleProfile.ProfileActivated()
        {
            var categoryName = "NServiceBus";
            var counterName = "Critical Time";
            PerformanceCounter counter = null;

            try
            {
                counter = new PerformanceCounter(categoryName, counterName, Program.EndpointId, false);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("NServiceBus performance counters not set up correctly. Running this process with the flag NServiceBus.InstallPerformanceCounters once should rectify this problem.", e);
            }

            Configure.ConfigurationComplete += 
                (o, e) =>
                {
                    var transport = Configure.Instance.Builder.Build<ITransport>();
                    transport.TransportMessageReceived += 
                        (obj, args) =>
                        {
                            counter.RawValue =
                                Convert.ToInt32((DateTime.Now - args.Message.TimeSent).TotalSeconds);
                        };
                };
        }
    }
}