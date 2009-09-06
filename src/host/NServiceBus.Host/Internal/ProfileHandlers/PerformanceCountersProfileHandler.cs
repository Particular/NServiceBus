using System;
using System.Diagnostics;
using NServiceBus.Host.Profiles;
using NServiceBus.Unicast.Transport.Msmq;

namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Handles the PerformanceCounters profile.
    /// </summary>
    public class PerformanceCountersProfileHandler : IHandleProfile<PerformanceCounters>
    {
        public void Init(IConfigureThisEndpoint specifier)
        {
            var categoryName = "NServiceBus";
            var counterName = "Critical Time";

            try
            {
                PerformanceCounterCategory.Delete(categoryName);
            }
// ReSharper disable EmptyGeneralCatchClause
            catch (Exception)
// ReSharper restore EmptyGeneralCatchClause
            {
                //ignore
            }

            var data = new CounterCreationDataCollection();

            var c1 = new CounterCreationData(counterName, "Age of the oldest message in the queue",
                                             PerformanceCounterType.NumberOfItems32);
            data.Add(c1);

            PerformanceCounterCategory.Create(categoryName, "NServiceBus statistics",
                                              PerformanceCounterCategoryType.MultiInstance, data);

            var counter = new PerformanceCounter(categoryName, counterName, Program.GetEndpointId(specifier.GetType()),
                                                 false);

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
