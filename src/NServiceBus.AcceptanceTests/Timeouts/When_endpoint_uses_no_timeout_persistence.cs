namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using AcceptanceTesting;
    using EndpointTemplates;
    using AcceptanceTesting.Support;
    using Config.ConfigurationSource;
    using Hosting.Helpers;
    using NUnit.Framework;

    public class When_endpoint_uses_no_timeout_persistence : NServiceBusAcceptanceTest
    {
        [Test]
        public void Endpoint_should_start()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsTrue(context.EndpointsStarted);
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<MinimalServer>(config =>
                {
                    config.DisableTimeoutManager();
                    Configure.Transactions.Advanced(s => s.DisableDistributedTransactions());
                });
            }
        }

        public class Context : ScenarioContext
        {
        }

        public class MinimalServer : IEndpointSetupTemplate
        {
            public Configure GetConfiguration(RunDescriptor runDescriptor, EndpointConfiguration endpointConfiguration, IConfigurationSource configSource)
            {
                var settings = runDescriptor.Settings;

                SetLoggingLibrary.Log4Net(null, new ContextAppender(runDescriptor.ScenarioContext, endpointConfiguration));


                var types = GetTypesToUse(endpointConfiguration);

                var config = Configure.With(types)
                    .CustomConfigurationSource(configSource)
                    .DefineBuilder(settings.GetOrNull("Builder"))
                    .DefineSerializer(settings.GetOrNull("Serializer"))
                    .DefineTransport(settings);

                return config.UnicastBus();
            }

            static IEnumerable<Type> GetTypesToUse(EndpointConfiguration endpointConfiguration)
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
}