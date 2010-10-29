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
            SetLoggingLibrary.Log4Net<AzureAppender>(null,
                a =>
                {
                    a.ScheduledTransferPeriod = 10;                    
                });
        }
    }
}