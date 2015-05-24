﻿namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_sending_from_a_send_only_new : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_the_message()
        {
            var context = new Context
            {
                Id = Guid.NewGuid()
            };
            Scenario.Define(context)
                    .WithEndpoint<Sender>(b => b.Given((bus, c) => bus.Send(new MyMessage
                    {
                        Id = c.Id
                    })))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.WasCalled)
                    .Run();

            Assert.True(context.WasCalled, "The message handler should be called");
        }

        [Test]
        public void Should_not_need_audit_or_fault_forwarding_config_to_start()
        {
            var context = new Context
            {
                Id = Guid.NewGuid()
            };
            Scenario.Define(context)
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

                    RegisterStartupTask<MyTask>();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                }

                public class MyTask : FeatureStartupTask
                {
                    public Context Context { get; set; }

                    protected override void OnStart()
                    {
                        Context.SendOnlyEndpointWasStarted = true;
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

        public class MyMessageHandler : IProcessCommands<MyMessage>
        {
            public Context Context { get; set; }

            public IBus Bus { get; set; }

            public void Handle(MyMessage message, ICommandContext context)
            {
                if (Context.Id != message.Id)
                    return;

                Context.WasCalled = true;
            }
        }
    }

    
}
