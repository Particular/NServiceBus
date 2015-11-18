namespace NServiceBus.AcceptanceTests.Hosting
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_feature_overrides_hostid : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task MD5_should_not_be_used()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MyEndpoint>(e => e.When(b => b.SendLocal(new MyMessage())))
                .Done(c => c.Done)
                .Run();

            Assert.IsTrue(context.NotSet);
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(Guid.NewGuid()));
            }
        }

        public class MyFeatureThatOverridesHostInformationDefaults : Feature
        {

            public MyFeatureThatOverridesHostInformationDefaults()
            {
                EnableByDefault();
                DependsOn("UnicastBus");
                Defaults(s =>
                {
                    // remove the override, we need to hack it via reflection!
                    var fieldInfo = s.GetType().GetField("Overrides", BindingFlags.Instance | BindingFlags.NonPublic);
                    var dictionary = (ConcurrentDictionary<string, object>)fieldInfo.GetValue(s);
                    object s2;
                    dictionary.TryRemove("NServiceBus.HostInformation.HostId", out s2);

                    // Try to get value, setting should not exist
                    var context = s.Get<Context>();
                    context.NotSet = !s.HasSetting("NServiceBus.HostInformation.HostId");

                    // Set override again so we have something
                    s.Set("NServiceBus.HostInformation.HostId", Guid.NewGuid());
                });
            }

            protected override void Setup(FeatureConfigurationContext context)
            {
            }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                Context.Done = true;

                return Task.FromResult(0);
            }
        }

        public class Context : ScenarioContext
        {
            public bool NotSet { get; set; }
            public bool Done { get; set; }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }
    }
}