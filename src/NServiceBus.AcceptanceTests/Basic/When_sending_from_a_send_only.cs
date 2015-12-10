namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_sending_from_a_send_only : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<Sender>(b => b.When((bus, c) => bus.Send(new MyMessage
                    {
                        Id = c.Id
                    })))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.WasCalled)
                    .Run();

            Assert.True(context.WasCalled, "The message handler should be called");
        }

        [Test]
        public async Task Should_not_need_audit_or_fault_forwarding_config_to_start()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<SendOnlyEndpoint>()
                    .Done(c => c.SendOnlyEndpointWasStarted)
                    .Run();

            Assert.True(context.SendOnlyEndpointWasStarted, "The endpoint should have started without any errors");
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public Guid Id { get; set; }

            public bool SendOnlyEndpointWasStarted { get; set; }
        }

        public class SendOnlyEndpoint : EndpointConfigurationBuilder
        {
            public SendOnlyEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<Bootstrapper>();
                }).SendOnly();
            }

            public class Bootstrapper : Feature
            {
                public Bootstrapper()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.Container.ConfigureComponent<MyTask>(DependencyLifecycle.SingleInstance);

                    context.RegisterStartupTask(b => b.Build<MyTask>());
                }

                public class MyTask : FeatureStartupTask
                {
                    readonly Context scenarioContext;

                    public MyTask(Context scenarioContext)
                    {
                        this.scenarioContext = scenarioContext;
                    }

                    protected override Task OnStart(IBusSession session)
                    {
                        scenarioContext.SendOnlyEndpointWasStarted = true;
                        return Task.FromResult(0);
                    }

                    protected override Task OnStop(IBusSession session)
                    {
                        return Task.FromResult(0);
                    }
                }
            }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>()
                    .SendOnly()
                    .AddMapping<MyMessage>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
            public Guid Id { get; set; }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                if (Context.Id != message.Id)
                    return Task.FromResult(0);

                Context.WasCalled = true;
                return Task.FromResult(0);
            }
        }
    }


}
