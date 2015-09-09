namespace NServiceBus.AcceptanceTests.HostInformation
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_feature_overrides_hostid : NServiceBusAcceptanceTest
    {

        [Test]
        public void MD5_should_not_be_used()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<MyEndpoint>(e => e.Given(b => b.SendLocal(new MyMessage())))
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
            bool notSet;

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
                    notSet = !s.HasSetting("NServiceBus.HostInformation.HostId");

                    // Set override again so we have something
                    s.Set("NServiceBus.HostInformation.HostId", Guid.NewGuid());

                });
            }

            protected override void Setup(FeatureConfigurationContext context)
            {
                context.Container.ConfigureProperty<Context>(c => c.NotSet, notSet);
            }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public void Handle(MyMessage message)
            {
                Context.Done = true;
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
