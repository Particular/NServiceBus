namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;

    class PerformanceCountersFeature : Feature
    {
        public PerformanceCountersFeature()
        {
            Prerequisite(c => !c.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "No performance counters applicable for send only endpoints");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var localAddress = context.Settings.LocalAddress();

            var messagesPulledFromQueueCounter = InstantiatePerformanceCounter("# of msgs pulled from the input queue /sec", localAddress);
            var successRateCounter = InstantiatePerformanceCounter("# of msgs successfully processed / sec", localAddress);
            var failureRateCounter = InstantiatePerformanceCounter("# of msgs failures / sec", localAddress);

            var criticalTimeCounter = InstantiatePerformanceCounter("Critical Time", context.Settings.EndpointName().ToString());
            var criticalTimeCalculator = new CriticalTimeCalculator(criticalTimeCounter);

            var disposables = new List<IDisposable>
            {
                messagesPulledFromQueueCounter,
                successRateCounter,
                failureRateCounter,
                criticalTimeCalculator
            };

            TimeSpan endpointSla;
            EstimatedTimeToSLABreachCalculator timeToSLABreachCalculator = null;

            if (context.Settings.TryGet(EndpointSLAKey, out endpointSla))
            {
                var slaBreachCounter = InstantiatePerformanceCounter("SLA violation countdown", context.Settings.EndpointName().ToString());
                timeToSLABreachCalculator = new EstimatedTimeToSLABreachCalculator(endpointSla, slaBreachCounter);
                disposables.Add(timeToSLABreachCalculator);
            }

            context.RegisterStartupTask(new DisposeCountersOnShutdown(disposables));
            context.Container.ConfigureComponent<PerformanceMonitorUsersInstaller.Installer>(DependencyLifecycle.SingleInstance);

            context.Settings.Get<NotificationSubscriptions>()
                .Subscribe<ReceivePipelineCompleted>(e =>
                {
                    //todo: need to check if the message failed or not
                    successRateCounter.Increment();

                    string timeSentHeader;
                    if (!e.ProcessedMessage.Headers.TryGetValue(Headers.TimeSent, out timeSentHeader))
                    {
                        return TaskEx.CompletedTask;
                    }

                    var timeSent = DateTimeExtensions.ToUtcDateTime(timeSentHeader);
                    var processingStarted = e.StartedAt;
                    var processingCompleted = e.CompletedAt;

                    timeToSLABreachCalculator?.Update(timeSent, processingStarted, processingCompleted);
                    criticalTimeCalculator.Update(timeSent, processingStarted, processingCompleted);

                    return TaskEx.CompletedTask;
                });
        }

        static PerformanceCounter InstantiatePerformanceCounter(string counterName, string instanceName)
        {
            if (instanceName.Length > 128)
            {
                throw new Exception($"The endpoint name ('{instanceName}') is too long (longer then {(int)sbyte.MaxValue}) to register as a performance counter instance name. Reduce the endpoint name.");
            }

            var message = $"NServiceBus performance counter for '{counterName}' is not set up correctly. To rectify this problem download the latest powershell commandlets from http://www.particular.net downloads page.";

            try
            {
                return new PerformanceCounter("NServiceBus", counterName, instanceName, false);
            }
            catch (Exception ex)
            {
                throw new Exception(message, ex);
            }
        }

        public const string EndpointSLAKey = "EndpointSLA";

        class DisposeCountersOnShutdown : FeatureStartupTask
        {
            public DisposeCountersOnShutdown(List<IDisposable> counters)
            {
                this.counters = counters;
            }

            protected override Task OnStart(IMessageSession session)
            {
                return TaskEx.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session)
            {
                counters.ForEach(c => c.Dispose());
                return TaskEx.CompletedTask;
            }

            List<IDisposable> counters;
        }
    }
}

namespace NServiceBus.Features
{
    /// <summary>
    /// Used to configure CriticalTimeMonitoring.
    /// </summary>
    public class CriticalTimeMonitoring
    {
    }
}

namespace NServiceBus.Features
{
    /// <summary>
    /// Used to configure SLAMonitoring.
    /// </summary>
    public class SLAMonitoring
    {
    }
}
namespace NServiceBus
{
    using Features;

    /// <summary>
    /// </summary>
    public static class PerformanceCountersConfig
    {
        /// <summary>
        /// Enables the NServiceBus specific performance counters.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static void EnablePerformanceCounters(this EndpointConfiguration config)
        {
            config.EnableFeature<PerformanceCountersFeature>();
        }
    }
}
namespace NServiceBus
{
    /// <summary>
    /// </summary>
    public static class CriticalTimeMonitoringConfig
    {
        /// <summary>
        /// Enables the NServiceBus specific performance counters.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static void EnableCriticalTimePerformanceCounter(this EndpointConfiguration config)
        {
        }
    }
}

namespace NServiceBus
{
    using System;
    using Features;

    /// <summary>
    /// Provide configuration options for monitoring related settings.
    /// </summary>
    public static class SLAMonitoringConfig
    {
        /// <summary>
        /// Enables the time to breach endpoint SLA performance counter.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="sla">The critical time SLA for this endpoint. Must be greater than <see cref="TimeSpan.Zero" />.</param>
        public static void EnableSLAPerformanceCounter(this EndpointConfiguration config, TimeSpan sla)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNegativeAndZero(nameof(sla), sla);

            config.Settings.Set(PerformanceCountersFeature.EndpointSLAKey, sla);

            config.EnablePerformanceCounters();
        }

        /// <summary>
        /// Enables the NServiceBus specific performance counters with a specific EndpointSLA.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static void EnableSLAPerformanceCounter(this EndpointConfiguration config)
        {
            //huh??
        }
    }
}