namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using AcceptanceTesting.Support;
    using Config.ConfigurationSource;
    using Hosting.Helpers;
    using Logging;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;

    public class DefaultServer : IEndpointSetupTemplate
    {
        readonly List<Type> typesToInclude;

        public DefaultServer()
        {
            typesToInclude = new List<Type>();
        }

        public DefaultServer(List<Type> typesToInclude)
        {
            this.typesToInclude = typesToInclude;
        }

        public Configure GetConfiguration(RunDescriptor runDescriptor, EndpointConfiguration endpointConfiguration, IConfigurationSource configSource, Action<ConfigurationBuilder> configurationBuilderCustomization)
        {
            var settings = runDescriptor.Settings;

            LogManager.UseFactory(new ContextAppender(runDescriptor.ScenarioContext));

            var types = GetTypesScopedByTestClass(endpointConfiguration);

            typesToInclude.AddRange(types);

            var config = Configure.With(o =>
            {
                configurationBuilderCustomization(o);
                o.EndpointName(endpointConfiguration.EndpointName);
                o.TypesToScan(typesToInclude);
                o.CustomConfigurationSource(configSource);
                o.EnableInstallers();
                o.DefineTransport(settings);
                o.RegisterComponents(r =>
                {
                    r.RegisterSingleton(runDescriptor.ScenarioContext.GetType(), runDescriptor.ScenarioContext);
                    r.RegisterSingleton(typeof(ScenarioContext), runDescriptor.ScenarioContext);
                });

                string selectedBuilder;
                if (settings.TryGetValue("Builder", out selectedBuilder))
                {
                    o.UseContainer(Type.GetType(selectedBuilder));
                }
                var serializer = settings.GetOrNull("Serializer");

                if (serializer != null)
                {
                    o.UseSerialization(Type.GetType(serializer));
                }
                o.DefinePersistence(settings);
            });

            config.Settings.SetDefault("ScaleOut.UseSingleBrokerQueue", true);

            return config;
        }

        static IEnumerable<Type> GetTypesScopedByTestClass(EndpointConfiguration endpointConfiguration)
        {
            var assemblies = new AssemblyScanner().GetScannableAssemblies();

            var types = assemblies.Assemblies
                //exclude all test types by default
                                  .Where(a => a != Assembly.GetExecutingAssembly())
                                  .SelectMany(a => a.GetTypes());


            types = types.Union(GetNestedTypeRecursive(endpointConfiguration.BuilderType.DeclaringType, endpointConfiguration.BuilderType));

            types = types.Union(endpointConfiguration.TypesToInclude);

            return types.Where(t => !endpointConfiguration.TypesToExclude.Contains(t)).ToList();
        }

        static IEnumerable<Type> GetNestedTypeRecursive(Type rootType, Type builderType)
        {
            yield return rootType;

            if (typeof(IEndpointConfigurationFactory).IsAssignableFrom(rootType) && rootType != builderType)
                yield break;

            foreach (var nestedType in rootType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SelectMany(t => GetNestedTypeRecursive(t, builderType)))
            {
                yield return nestedType;
            }
        }

    }
}