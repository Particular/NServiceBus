using NServiceBus.Config;
using NServiceBus.Integration.Azure;
using log4net.Layout;
using log4net.Core;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Reflection;
using System;

namespace NServiceBus.Hosting.Azure.LoggingHandlers
{
    /// <summary>
    /// Handles logging configuration for the production profile
    /// </summary>
    public class ProductionLoggingHandler : IConfigureLoggingForProfile<Production>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
            if (Configure.Instance == null)
                Configure.With();

            if (Configure.Instance.Configurer == null || Configure.Instance.Builder == null)
                Configure.Instance.DefaultBuilder();

            Configure.Instance
                .AzureConfigurationSource()
                .Log4Net<AzureAppender>(
                a =>
                {
                    a.ScheduledTransferPeriod = 10;                    
                });
        }
    }
}