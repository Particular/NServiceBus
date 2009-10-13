using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Logging;
using NServiceBus.Host.Internal;
using Topshelf;
using Topshelf.Configuration;
using System.Configuration;
using Topshelf.Internal.ArgumentParsing;

namespace NServiceBus.Host
{
    /// <summary>
    /// Entry point to the process.
    /// </summary>
    public class Program
    {
        private static void Main(string[] args)
        {
            InitializeLogging();

            Type endpointConfigurationType = GetEndpointConfigurationType();

            AssertThatEndpointConfigurationTypeHasDefaultConstructor(endpointConfigurationType);

            string endpointConfigurationFile = GetEndpointConfigurationFile(endpointConfigurationType);

            if (!File.Exists(endpointConfigurationFile))
            {
                throw new InvalidOperationException("No configuration file found at: " + endpointConfigurationFile);
            }

            var endpointConfiguration = Activator.CreateInstance(endpointConfigurationType);

            EndpointId = GetEndpointId(endpointConfiguration);

            AppDomain.CurrentDomain.SetupInformation.AppDomainInitializerArguments = args;

            IRunConfiguration cfg = RunnerConfigurator.New(x =>
            {
                x.SetDisplayName(EndpointId);
                x.SetServiceName(EndpointId);
                x.SetDescription("NServiceBus Message Endpoint Host Service");

                x.ConfigureServiceInIsolation<GenericHost>(endpointConfigurationType.AssemblyQualifiedName, c =>
                {
                    c.ConfigurationFile(endpointConfigurationFile);
                    c.CommandLineArguments(args, () => SetHostServiceLocatorArgs);
                    c.WhenStarted(service => service.Start());
                    c.WhenStopped(service => service.Stop());
                    c.CreateServiceLocator(() => new HostServiceLocator());
                });

                if (!(endpointConfiguration is ISpecify.ToStartAutomatically))
                {
                    x.DoNotStartAutomatically();
                }

                var parser = new ArgumentParser();
                var arguments = parser.Parse(args);
                var username = arguments.SingleOrDefault(argument => argument.Key == "username");
                var password = arguments.SingleOrDefault(argument => argument.Key == "password");

                if (username != null && password != null)
                {
                    x.RunAs(username.Value, password.Value);
                }
                else
                {
                    x.RunAsLocalSystem();
                }
            });

            Runner.Host(cfg, args);
        }

        /// <summary>
        /// Gives an identifier for this endpoint
        /// </summary>
        public static string EndpointId { get; set; }

        private static void SetHostServiceLocatorArgs(string[] args)
        {
            HostServiceLocator.Args = args;
        }

        private static void AssertThatEndpointConfigurationTypeHasDefaultConstructor(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
                throw new InvalidOperationException("Endpoint configuration type needs to have a default constructor: " + type.FullName);
        }

        private static string GetEndpointConfigurationFile(Type endpointConfigurationType)
        {
            return Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                endpointConfigurationType.Assembly.ManifestModule.Name + ".config");
        }

        /// <summary>
        /// Gives a string which serves to identify the endpoint.
        /// </summary>
        /// <param name="endpointConfiguration"></param>
        /// <returns></returns>
        public static string GetEndpointId(object endpointConfiguration)
        {
            string endpointName = GetEndpointName(endpointConfiguration);
            return string.Format("{0}_v{1}", endpointName, endpointConfiguration.GetType().Assembly.GetName().Version);
        }

        private static Type GetEndpointConfigurationType()
        {
            string endpoint = ConfigurationManager.AppSettings["EndpointConfigurationType"];
            if (endpoint != null)
                return Type.GetType(endpoint, true);

            IEnumerable<Type> endpoints = ScanAssembliesForEndpoints();

            ValidateEndpoints(endpoints);

            return endpoints.First();
        }

        private static IEnumerable<Type> ScanAssembliesForEndpoints()
        {
            foreach (var assembly in AssemblyScanner.GetScannableAssemblies())
                foreach (Type type in assembly.GetTypes().Where(t => typeof(IConfigureThisEndpoint).IsAssignableFrom(t) && t != typeof(IConfigureThisEndpoint)))
                {
                    yield return type;
                }
        }

        private static void ValidateEndpoints(IEnumerable<Type> endpointConfigurationTypes)
        {
            if (endpointConfigurationTypes.Count() == 0)
            {
                throw new InvalidOperationException("No endpoint configuration found in scanned assemlies. " +
                    "This usually happens when NServiceBus fails to load your assembly contaning IConfigureThisEndpoint." +
                    " Please enable Trace in NServiceBus.Host.exe.config to debug loader exceptions, " +
                    "Scanned path: " + AppDomain.CurrentDomain.BaseDirectory);
            }

            if (endpointConfigurationTypes.Count() > 1)
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

        private static string GetEndpointName(object endpointConfiguration)
        {
            string endpointName = null;

            var iHaveEndpointName = endpointConfiguration as ISpecify.EndpointName;
            if (iHaveEndpointName != null)
            {
                endpointName = iHaveEndpointName.EndpointName;
            }

            if (!string.IsNullOrEmpty(endpointName))
            {
                return endpointName;
            }

            return endpointConfiguration.GetType().FullName;
        }


        private static void InitializeLogging()
        {
            var props = new NameValueCollection();
            props["configType"] = "EXTERNAL";
            LogManager.Adapter = new Common.Logging.Log4Net.Log4NetLoggerFactoryAdapter(props);

            var layout = new log4net.Layout.PatternLayout("%d [%t] %-5p %c [%x] <%X{auth}> - %m%n");
            var level = log4net.Core.Level.Warn;

            var appender = new log4net.Appender.RollingFileAppender
            {
                Layout = layout,
                Threshold = level,
                CountDirection = 1,
                DatePattern = "yyyy-mm-dd",
                RollingStyle = log4net.Appender.RollingFileAppender.RollingMode.Composite,
                MaxFileSize = 1024 * 1024,
                MaxSizeRollBackups = 10,
                LockingModel = new log4net.Appender.FileAppender.MinimalLock(),
                StaticLogFileName = true,
                File = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hostlogfile"),
                AppendToFile = true
            };
            appender.ActivateOptions();

            log4net.Config.BasicConfigurator.Configure(appender);
        }


    }
}