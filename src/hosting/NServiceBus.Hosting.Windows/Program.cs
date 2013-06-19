namespace NServiceBus.Hosting.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Linq;
    using Arguments;
    using Helpers;
    using Installers;
    using Magnum.StateMachine;
    using Topshelf;
    using Topshelf.Configuration;

    /// <summary>
    /// Entry point to the process.
    /// </summary>
    public class Program
    {
        private static AssemblyScannerResults assemblyScannerResults;

        static void Main(string[] args)
        {
            var arguments = new HostArguments(args);

            if (arguments.Help)
            {
                arguments.PrintUsage();
                return;
            }
            
            assemblyScannerResults = AssemblyScanner.GetScannableAssemblies();

            var endpointTypeDeterminer = new EndpointTypeDeterminer(assemblyScannerResults, () => ConfigurationManager.AppSettings["EndpointConfigurationType"]);
            var endpointConfigurationType = endpointTypeDeterminer.GetEndpointConfigurationTypeForHostedEndpoint(arguments);

            var endpointConfigurationFile = endpointConfigurationType.EndpointConfigurationFile;
            var endpointName = endpointConfigurationType.EndpointName;
            var serviceName = endpointConfigurationType.ServiceName;
            var endpointVersion = endpointConfigurationType.EndpointVersion;
            var displayName = serviceName + "-" + endpointVersion;

            if (arguments.SideBySide)
            {
                serviceName += "-" + endpointVersion;
            }

            //Add the endpoint name so that the new appdomain can get it
            if (arguments.EndpointName == null && !String.IsNullOrEmpty(endpointName))
            {
                args = args.Concat(new[] { String.Format(@"/endpointName={0}", endpointName) }).ToArray();
            }

            //Add the ScannedAssemblies name so that the new appdomain can get it
            if (arguments.ScannedAssemblies.Count == 0)
            {
                args = assemblyScannerResults.Assemblies.Select(s => s.ToString()).Aggregate(args, (current, result) => current.Concat(new[] { String.Format(@"/scannedAssemblies={0}", result) }).ToArray());
            }

            //Add the endpointConfigurationType name so that the new appdomain can get it
            if (arguments.EndpointConfigurationType == null)
            {
                args = args.Concat(new[] { String.Format(@"/endpointConfigurationType={0}", endpointConfigurationType.AssemblyQualifiedName) }).ToArray();
            }

            if (arguments.Install)
            {
                WindowsInstaller.Install(args, endpointConfigurationFile);
            }
            
            IRunConfiguration cfg = RunnerConfigurator.New(x =>
                                                               {
                                                                   x.ConfigureServiceInIsolation<WindowsHost>(endpointConfigurationType.AssemblyQualifiedName, c =>
                                                                    {
                                                                        c.ConfigurationFile(endpointConfigurationFile);
                                                                        c.WhenStarted(service => service.Start());
                                                                        c.WhenStopped(service => service.Stop());
                                                                        c.CommandLineArguments(args, () => SetHostServiceLocatorArgs);
                                                                        c.CreateServiceLocator(() => new HostServiceLocator());
                                                                    });

                                                                   if (arguments.Username != null && arguments.Password != null)
                                                                   {
                                                                       x.RunAs(arguments.Username, arguments.Password);
                                                                   }
                                                                   else
                                                                   {
                                                                       x.RunAsLocalSystem();
                                                                   }

                                                                   if (arguments.StartManually)
                                                                   {
                                                                       x.DoNotStartAutomatically();
                                                                   }

                                                                   x.SetDisplayName(arguments.DisplayName ?? displayName);
                                                                   x.SetServiceName(serviceName);
                                                                   x.SetDescription(arguments.Description ?? string.Format("NServiceBus Endpoint Host Service for {0}", displayName));

                                                                   var serviceCommandLine = new List<string>();

                                                                   if (!String.IsNullOrEmpty(arguments.EndpointConfigurationType))
                                                                   {
                                                                       serviceCommandLine.Add(String.Format(@"/endpointConfigurationType:""{0}""", arguments.EndpointConfigurationType));
                                                                   }
                                                                   
                                                                   if (!String.IsNullOrEmpty(endpointName))
                                                                   {
                                                                       serviceCommandLine.Add(String.Format(@"/endpointName:""{0}""", endpointName));
                                                                   }

                                                                   if (!String.IsNullOrEmpty(serviceName))
                                                                   {
                                                                       serviceCommandLine.Add(String.Format(@"/serviceName:""{0}""", serviceName));
                                                                   }

                                                                   if (arguments.ScannedAssemblies.Count > 0)
                                                                   {
                                                                       serviceCommandLine.AddRange(arguments.ScannedAssemblies.Select(assembly => String.Format(@"/scannedAssemblies:""{0}""", assembly)));
                                                                   }

                                                                   if (arguments.OtherArgs.Any())
                                                                   {
                                                                       serviceCommandLine.AddRange(arguments.OtherArgs);
                                                                   }

                                                                   var commandLine = String.Join(" ", serviceCommandLine);
                                                                   x.SetServiceCommandLine(commandLine);

                                                                   if (arguments.DependsOn == null)
                                                                       x.DependencyOnMsmq();
                                                                   else
                                                                       foreach (var dependency in arguments.DependsOn)
                                                                           x.DependsOn(dependency);
                                                               });
            try
            {

                Runner.Host(cfg, args);
            }
            catch (StateMachineException exception)
            {
                var innerException = exception.InnerException;
                innerException.PreserveStackTrace();
                throw innerException;
            }
        }

        static void SetHostServiceLocatorArgs(string[] args)
        {
            HostServiceLocator.Args = args;
        }
    }
}
