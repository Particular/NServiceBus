namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.ServiceProcess;
    using Logging;
    using NServiceBus.Outbox;
    using Pipeline;
    using Transports;

    /// <summary>
    /// Configure the Outbox.
    /// </summary>
    public class Outbox : Feature
    {
        internal Outbox()
        {
            var rabbit = Type.GetType("NServiceBus.Features.RabbitMqTransport, NServiceBus.Transports.RabbitMQ", false);
            var azureServiceBus = Type.GetType("NServiceBus.Features.AzureServiceBusTransport, NServiceBus.Azure.Transports.WindowsAzureServiceBus", false);
            var azureStorageQueue = Type.GetType("NServiceBus.Features.AzureStorageQueueTransport, NServiceBus.Azure.Transports.WindowsAzureStorageQueues", false);

            var dependencies = new List<Type>();

            if (rabbit != null)
            {
                dependencies.Add(rabbit);
            }
            if (azureServiceBus != null)
            {
                dependencies.Add(azureServiceBus);
            }
            if (azureStorageQueue != null)
            {
                dependencies.Add(azureStorageQueue);
            }

            if (dependencies.Count > 0)
            {
                DependsOnAtLeastOne(dependencies.ToArray());
                EnableByDefault();
            }
            else
            {
                var sql = Type.GetType("NServiceBus.Features.SqlServerTransport, NServiceBus.Transports.SQLServer", false);

                if (sql != null)
                {
                    dependencies.Add(sql);                        
                }

                dependencies.Add(typeof(MsmqTransport));

                DependsOnAtLeastOne(dependencies.ToArray());

                Prerequisite(context =>
                {
                    var configValue = ConfigurationManager.AppSettings.Get("NServiceBus/Outbox");

                    if (configValue == null)
                    {
                        return false;
                    }
                    
                    bool result;

                    Boolean.TryParse(configValue, out result);

                    return result;
                });
            }

            RegisterStartupTask<DtcRunningWarning>();
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register<OutboxDeduplicationRegistration>();
            context.Pipeline.Register<OutboxRecorderRegistration>();
            context.Pipeline.Replace(WellKnownBehavior.DispatchMessageToTransport, typeof(OutboxSendBehavior), "Sending behavior with a delay sending until all business transactions are committed to the outbox storage");

            //make the audit use the outbox as well
            if (context.Container.HasComponent<IAuditMessages>())
            {
                context.Container.ConfigureComponent<OutboxAwareAuditer>(DependencyLifecycle.InstancePerCall);
            }
        }

        class OutboxDeduplicationRegistration : RegisterBehavior
        {
            public OutboxDeduplicationRegistration()
                : base("OutboxDeduplication", typeof(OutboxDeduplicationBehavior), "Deduplication for the outbox feature")
            {
                InsertAfter(WellKnownBehavior.ChildContainer);
                InsertBefore(WellKnownBehavior.UnitOfWork);
            }
        }

        class OutboxRecorderRegistration : RegisterBehavior
        {
            public OutboxRecorderRegistration()
                : base("OutboxRecorder", typeof(OutboxRecordBehavior), "Records all action to the outbox storage")
            {
                InsertBefore(WellKnownBehavior.MutateIncomingTransportMessage);
                InsertAfter(WellKnownBehavior.UnitOfWork);
            }
        }
    }

    class DtcRunningWarning :FeatureStartupTask
    {
        protected override void OnStart()
        {
            try
            {
                var sc = new ServiceController
                {
                    ServiceName = "MSDTC",
                    MachineName = "."
                };

                if (sc.Status == ServiceControllerStatus.Running)
                {
                    log.Warn(@"We have detected that MSDTC service is running on your machine.
Because you have configured this endpoint to run with Outbox enabled we recommend turning MSDTC off to ensure that the Outbox behavior is working as expected and no other resources are enlisting in distributed transactions.");
                }
            }
            catch (Exception)
            {
                // Ignore if we can't check it.
            }
            
        }

        static ILog log = LogManager.GetLogger<DtcRunningWarning>();
    }
}