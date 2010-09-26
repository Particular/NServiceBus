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
                    a.Threshold = DetermineTreshold();
                    a.Layout = new PatternLayout("%d [%t] %-5p %c [%x] <%X{auth}> - %m%n");
                    a.ScheduledTransferPeriod = TimeSpan.FromMinutes(10);                    
                });
        }

        private static Level DetermineTreshold()
        {
            try
            {
                var threshold = RoleEnvironment.GetConfigurationSettingValue("Diagnostics.LogLevel");
                foreach (var f in typeof(Level).GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (string.Compare(threshold, f.Name, true) != 0) continue;

                    var val = f.GetValue(null);
                    return val as Level;
                }
            }
            catch
            { } // occurs if the Diagnostics level is not set, or set to a wrong value

            return Level.Info;
        }
    }
}