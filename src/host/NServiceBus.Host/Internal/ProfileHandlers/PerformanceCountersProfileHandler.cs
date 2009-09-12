using System;
using System.Diagnostics;
using NServiceBus.Unicast.Transport.Msmq;

namespace NServiceBus.Host.Internal.ProfileHandlers
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
                throw new InvalidOperationException("NServiceBus performance counters not set up correctly. Running this process with the flag /InstallPerformanceCounters once should rectify this problem.", e);
            }

            GenericHost.ConfigurationComplete += (o, e) =>
                                                     {
                     var msmqTransport = Configure.ObjectBuilder.Build<MsmqTransport>();
                     msmqTransport.TransportMessageReceived += (obj, args) =>
                       {
                           counter.RawValue =
                               Convert.ToInt32((DateTime.Now - args.Message.TimeSent).TotalSeconds);

                       };
                 };
    }
    }
}
