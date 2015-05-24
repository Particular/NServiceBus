﻿namespace NServiceBus.AcceptanceTests.Callbacks
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_using_callbacks_with_messageid_eq_cid_new : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_trigger_the_callback()
        {
            var context = Scenario.Define<Context>()
                .WithEndpoint<EndpointWithLocalCallback>(b=>b.Given(async (bus,c)=>
                    {
                        var id = Guid.NewGuid().ToString();
                        var options = new SendLocalOptions(id)
                            .SetCustomMessageId(id);

                        await bus.RequestWithTransientlyHandledResponseAsync<MyResponse>(new MyRequest(), options);

                        c.CallbackFired = true;
                    }))
                .Done(c => c.CallbackFired)
                .Run();

            Assert.True(context.CallbackFired);
        }

        public class Context : ScenarioContext
        {
            public bool CallbackFired { get; set; }
        }

        public class EndpointWithLocalCallback : EndpointConfigurationBuilder
        {
            public EndpointWithLocalCallback()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyRequestHandler : IProcessCommands<MyRequest>
            {
                public Context Context { get; set; }

                public void Handle(MyRequest request, ICommandContext context)
                {
                    Assert.False(Context.CallbackFired);

                    context.Reply(new MyResponse());
                }
            }
        }
        public class MyRequest : IMessage{}

        public class MyResponse : IMessage { }
    }
}