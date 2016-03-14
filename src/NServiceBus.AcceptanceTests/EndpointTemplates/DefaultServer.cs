namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Features;
    using NServiceBus.Hosting.Helpers;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Serialization;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public DefaultServer()
        {
            typesToInclude = new List<Type>();
        }

        public DefaultServer(List<Type> typesToInclude)
        {
            this.typesToInclude = typesToInclude;
        }

        public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var settings = runDescriptor.Settings;

            var types = GetTypesScopedByTestClass(endpointConfiguration);

            typesToInclude.AddRange(types);

            var builder = new EndpointConfiguration(endpointConfiguration.EndpointName);

            builder.TypesToIncludeInScan(typesToInclude);
            builder.CustomConfigurationSource(configSource);
            builder.EnableInstallers();

            builder.DisableFeature<TimeoutManager>();
            builder.DisableFeature<SecondLevelRetries>();
            builder.DisableFeature<FirstLevelRetries>();

            await builder.DefineTransport(settings, endpointConfiguration.EndpointName).ConfigureAwait(false);

            builder.DefineBuilder(settings);
            builder.RegisterComponents(r => { RegisterInheritanceHierarchyOfContextOnContainer(runDescriptor, r); });

            Type serializerType;
            if (settings.TryGet("Serializer", out serializerType))
            {
                builder.UseSerialization((SerializationDefinition) Activator.CreateInstance(serializerType));
            }
            await builder.DefinePersistence(settings, endpointConfiguration.EndpointName).ConfigureAwait(false);

            builder.GetSettings().SetDefault("ScaleOut.UseSingleBrokerQueue", true);
            configurationBuilderCustomization(builder);

            return builder;
        }

        static void RegisterInheritanceHierarchyOfContextOnContainer(RunDescriptor runDescriptor, IConfigureComponents r)
        {
            var type = runDescriptor.ScenarioContext.GetType();
            while (type != typeof(object))
            {
                r.RegisterSingleton(type, runDescriptor.ScenarioContext);
                type = type.BaseType;
            }
        }

        static IEnumerable<Type> GetTypesScopedByTestClass(EndpointCustomizationConfiguration endpointConfiguration)
        {
            var assemblies = new AssemblyScanner().GetScannableAssemblies();

            var types = assemblies.Assemblies
                //exclude all test types by default
                .Where(a =>
                {
                    var references = a.GetReferencedAssemblies();

                    return references.All(an => an.Name != "nunit.framework");
                })
                .SelectMany(a => a.GetTypes());


            types = types.Union(GetNestedTypeRecursive(endpointConfiguration.BuilderType.DeclaringType, endpointConfiguration.BuilderType));

            types = types.Union(endpointConfiguration.TypesToInclude);

            return types.Where(t => !endpointConfiguration.TypesToExclude.Contains(t)).ToList();
        }

        static IEnumerable<Type> GetNestedTypeRecursive(Type rootType, Type builderType)
        {
            if (rootType == null)
            {
                throw new InvalidOperationException("Make sure you nest the endpoint infrastructure inside the TestFixture as nested classes");
            }

            yield return rootType;

            if (typeof(IEndpointConfigurationFactory).IsAssignableFrom(rootType) && rootType != builderType)
            {
                yield break;
            }

            foreach (var nestedType in rootType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SelectMany(t => GetNestedTypeRecursive(t, builderType)))
            {
                yield return nestedType;
            }
        }

        List<Type> typesToInclude;
    }
}