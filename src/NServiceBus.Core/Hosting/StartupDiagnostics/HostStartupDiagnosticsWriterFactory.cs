namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Logging;

    class HostStartupDiagnosticsWriterFactory
    {
        public static HostStartupDiagnosticsWriter GetDiagnosticsWriter(HostingComponent.Configuration configuration)
        {
            var diagnosticsWriter = configuration.HostDiagnosticsWriter;
            if (diagnosticsWriter == null)
            {
                diagnosticsWriter = BuildDefaultDiagnosticsWriter(configuration);
            }

            return new HostStartupDiagnosticsWriter(diagnosticsWriter, false);
        }

        static Func<string, Task> BuildDefaultDiagnosticsWriter(HostingComponent.Configuration configuration)
        {
            var diagnosticsRootPath = configuration.DiagnosticsPath;

            if (diagnosticsRootPath == null)
            {
                try
                {
                    diagnosticsRootPath = Path.Combine(Host.GetOutputDirectory(), ".diagnostics");
                }
                catch (Exception e)
                {
                    logger.Warn("Unable to determine the diagnostics output directory. Check the attached exception for further information, or configure a custom diagnostics directory using 'EndpointConfiguration.SetDiagnosticsPath()'.", e);

                    return data => Task.CompletedTask;
                }
            }

            if (!Directory.Exists(diagnosticsRootPath))
            {
                try
                {
                    Directory.CreateDirectory(diagnosticsRootPath);
                }
                catch (Exception e)
                {
                    logger.Warn("Unable to create the diagnostics output directory. Check the attached exception for further information, or change the diagnostics directory using 'EndpointConfiguration.SetDiagnosticsPath()'.", e);

                    return data => Task.CompletedTask;
                }
            }

            // Once we have the proper hosting model in place we can skip the endpoint name since the host would
            // know how to handle multi hosting but for now we do this so that multi-hosting users will get a file per endpoint
            var startupDiagnosticsFileName = $"{configuration.EndpointName}-configuration.txt";
            var startupDiagnosticsFilePath = Path.Combine(diagnosticsRootPath, startupDiagnosticsFileName);

            
            return data =>
            {
                var prettied = JsonPrettyPrinter.Print(data);    
                return AsyncFile.WriteText(startupDiagnosticsFilePath, prettied);
            };
        }

        static readonly ILog logger = LogManager.GetLogger<HostStartupDiagnosticsWriter>();
    }
}
