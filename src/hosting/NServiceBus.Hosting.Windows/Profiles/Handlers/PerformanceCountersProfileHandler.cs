using System;
using System.Diagnostics;
using System.Threading;
using NServiceBus.Hosting.Profiles;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    /// <summary>
    /// Handles the PerformanceCounters profile.
    /// </summary>
    public class PerformanceCountersProfileHandler : IHandleProfile<PerformanceCounters>, IDisposable
    {
        void IHandleProfile.ProfileActivated()
        {
            var categoryName = "NServiceBus";
            var counterName = "Critical Time";
            
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
                        transport.TransportMessageReceived += HandleTransportMessageReceived;
                    };

            timer = new Timer(ClearPerfCounter, null, 0, 2000);
        }

        private void ClearPerfCounter(object state)
        {
            var delta = DateTime.Now - timeOfLastCounter;

            if (delta > maxDelta)
                counter.RawValue = 0;
        }

        private void HandleTransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            counter.RawValue = Convert.ToInt32((DateTime.Now - e.Message.TimeSent).TotalSeconds);

            timeOfLastCounter = DateTime.Now;
        }

        public void Dispose()
        {
            timer.Dispose();
        }

        PerformanceCounter counter;
        Timer timer;
        private DateTime timeOfLastCounter;
        private readonly TimeSpan maxDelta = TimeSpan.FromSeconds(2);
    }
}