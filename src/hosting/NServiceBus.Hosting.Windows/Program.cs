namespace NServiceBus.Hosting.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using Arguments;
    using Helpers;
    using Installers;
    using Topshelf;
    using Topshelf.Configuration;
    using Utils;

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
            var endpointConfigurationType = GetEndpointConfigurationType(arguments);

            if (endpointConfigurationType == null)
            {
                throw new InvalidOperationException("No endpoint configuration found in scanned assemblies. " +
                    "This usually happens when NServiceBus fails to load your assembly containing IConfigureThisEndpoint." +
                    " Try specifying the type explicitly in the NServiceBus.Host.exe.config using the appsetting key: EndpointConfigurationType, " +
                    "Scanned path: " + AppDomain.CurrentDomain.BaseDirectory);
            }

            AssertThatEndpointConfigurationTypeHasDefaultConstructor(endpointConfigurationType);
            string endpointConfigurationFile = GetEndpointConfigurationFile(endpointConfigurationType);
            string endpointName, serviceName;
            var endpointVersion = FileVersionRetriever.GetFileVersion(endpointConfigurationType);

            GetEndpointName(endpointConfigurationType, arguments, out endpointName, out serviceName);

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

                                                                   if (arguments.ScannedAssemblies.Count > 0)
                                                                   {
                                                                       serviceCommandLine.AddRange(arguments.ScannedAssemblies.Select(assembly => String.Format(@"/scannedAssemblies:""{0}""", assembly)));
                                                                   }

                                                                   var commandLine = String.Join(" ", serviceCommandLine);
                                                                   x.SetServiceCommandLine(commandLine);

                                                                   if (arguments.DependsOn == null)
                                                                       x.DependencyOnMsmq();
                                                                   else
                                                                       foreach (var dependency in arguments.DependsOn)
                                                                           x.DependsOn(dependency);
                                                               });

            Runner.Host(cfg, args);
        }

        static void SetHostServiceLocatorArgs(string[] args)
        {
            HostServiceLocator.Args = args;
        }

        static void AssertThatEndpointConfigurationTypeHasDefaultConstructor(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
                throw new InvalidOperationException("Endpoint configuration type needs to have a default constructor: " + type.FullName);
        }

        static string GetEndpointConfigurationFile(Type endpointConfigurationType)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, endpointConfigurationType.Assembly.ManifestModule.Name + ".config");
        }


        private static void GetEndpointName(Type endpointConfigurationType, HostArguments arguments,
                                            out string endpointName, out string serviceName)
        {
            var endpointConfiguration = Activator.CreateInstance(endpointConfigurationType);

            endpointName = null;
            serviceName = endpointConfiguration.GetType().Namespace ?? endpointConfiguration.GetType().Assembly.GetName().Name;

            if (arguments.ServiceName != null)
            {
                serviceName = arguments.ServiceName;
            }

            var arr = endpointConfiguration.GetType().GetCustomAttributes(typeof (EndpointNameAttribute), false);
            if (arr.Length == 1)
            {
                endpointName = ((EndpointNameAttribute) arr[0]).Name;
                return;
            }

            var nameThisEndpoint = endpointConfiguration as INameThisEndpoint;
            if (nameThisEndpoint != null)
            {
                endpointName = nameThisEndpoint.GetName();
                return;
            }

            if (arguments.EndpointName != null)
            {
                endpointName = arguments.EndpointName;
            }
        }

        static Type GetEndpointConfigurationType(HostArguments arguments)
        {
            if (arguments.EndpointConfigurationType != null)
            {
                string t = arguments.EndpointConfigurationType;
                if (t != null)
                {
                    Type endpointType = Type.GetType(t, false);
                    if (endpointType == null)
                        throw new ConfigurationErrorsException(string.Format("Command line argument 'endpointConfigurationType' has specified to use the type '{0}' but that type could not be loaded.", t));

                    return endpointType;
                }
            }

            string endpoint = ConfigurationManager.AppSettings["EndpointConfigurationType"];
            if (endpoint != null)
            {
                var endpointType = Type.GetType(endpoint, false);
                if (endpointType == null)
                    throw new ConfigurationErrorsException(string.Format("The 'EndpointConfigurationType' entry in the NServiceBus.Host.exe.config has specified to use the type '{0}' but that type could not be loaded.", endpoint));

                return endpointType;
            }

            List<Type> endpoints = ScanAssembliesForEndpoints().ToList();
            if (!endpoints.Any())
            {
                Console.Out.WriteLine(assemblyScannerResults);
                return null;
            }

            AssertThatNotMoreThanOneEndpointIsDefined(endpoints);
            return endpoints.First();
        }

        static IEnumerable<Type> ScanAssembliesForEndpoints()
        {
            var scannableAssemblies = assemblyScannerResults.Assemblies;

            return scannableAssemblies.SelectMany(assembly => assembly.GetTypes().Where(
                t => typeof(IConfigureThisEndpoint).IsAssignableFrom(t)
                     && t != typeof(IConfigureThisEndpoint)
                     && !t.IsAbstract));
        }


        static void AssertThatNotMoreThanOneEndpointIsDefined(List<Type> endpointConfigurationTypes)
        {
            if (endpointConfigurationTypes.Count > 1)
            {
                throw new InvalidOperationException("Host doesn't support hosting of multiple endpoints. " +
                                                    "Endpoint classes found: " +
                                                    string.Join(", ",
                                                                endpointConfigurationTypes.Select(
                                                                    e => e.AssemblyQualifiedName).ToArray()) +
                                                    " You may have some old assemblies in your runtime directory." +
                                                    " Try right-clicking your VS project, and selecting 'Clean'."
                    );

            }
        }

    }
}
