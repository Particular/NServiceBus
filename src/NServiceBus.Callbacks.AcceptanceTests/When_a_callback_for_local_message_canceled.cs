namespace NServiceBus.AcceptanceTests.Callbacks
{
    using System;
    using System.Threading;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_a_callback_for_local_message_canceled : NServiceBusAcceptanceTest
    {
        [Test]
        public void ShouldNot_trigger_the_callback_when_canceled()
        {
            OperationCanceledException exception = null;
            Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithLocalCallback>(b => b.Given(async (bus, context) =>
                        {
                            var cs = new CancellationTokenSource();
                            context.TokenSource = cs;

                            var response = bus.RequestWithTransientlyHandledResponseAsync<MyResponse>(new MyRequest(), new SendLocalOptions().RegisterCancellationToken(cs.Token));

                            try
                            {
                                await response;
                                context.CallbackFired = true;
                            }
                            catch (OperationCanceledException e)
                            {
                                exception = e;
                            }
                        }))
                    .Done(c => exception != null || c.HandlerGotTheRequest)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c =>
                    {
                        Assert.False(c.CallbackFired);
                        Assert.True(c.HandlerGotTheRequest);
                    })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public CancellationTokenSource TokenSource { get; set; }

            public bool HandlerGotTheRequest { get; set; }

            public bool CallbackFired { get; set; }
        }

        public class EndpointWithLocalCallback : EndpointConfigurationBuilder
        {
            public EndpointWithLocalCallback()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyRequest request)
                {
                    Context.HandlerGotTheRequest = true;
                    Context.TokenSource.Cancel();

                    Bus.Reply(new MyResponse());
                }
            }
        }

        [Serializable]
        public class MyRequest : IMessage { }

        [Serializable]
        public class MyResponse : IMessage { }
    }
}