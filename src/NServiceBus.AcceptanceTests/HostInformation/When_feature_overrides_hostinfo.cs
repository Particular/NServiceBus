namespace NServiceBus.AcceptanceTests.HostInformation
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_feature_overrides_hostinfo : NServiceBusAcceptanceTest
    {
        static Guid hostId = new Guid("6c0f50de-dac9-4693-b138-6d1033c15ed6");
        static string instanceName = "Foo";

        [Test]
        public void HostInfo_is_changed()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<MyEndpoint>(e => e.Given(b => b.SendLocal(new MyMessage())))
                .Done(c => c.HostId != Guid.Empty)
                .Run();

            Assert.AreEqual(hostId, context.HostId);
            Assert.AreEqual(instanceName, context.HostDisplayName);
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>();
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
                    s.SetDefault("NServiceBus.HostInformation.HostId", hostId);
                    s.SetDefault("NServiceBus.HostInformation.DisplayName", instanceName);
                    s.SetDefault("NServiceBus.HostInformation.Properties", new Dictionary<string, string>
                    {
                        {"RoleName", "My role name"},
                        {"RoleInstanceId", "the role instance id"},
                    });
                });
            }

            protected override void Setup(FeatureConfigurationContext context)
            {
            }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public IBus Bus { get; set; }

            public Context Context { get; set; }

            public void Handle(MyMessage message)
            {
                Context.HostDisplayName = Bus.GetMessageHeader(message, Headers.HostDisplayName);
                Context.HostId = new Guid(Bus.GetMessageHeader(message, Headers.HostId));
            }
        }

        public class Context : ScenarioContext
        {
            public Guid HostId { get; set; }
            public string HostDisplayName { get; set; }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }
    }
}