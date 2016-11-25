namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using Features;
    using NServiceBus.Routing.Legacy;
    using NUnit.Framework;
    using ObjectBuilder;
    using Transport;

    public class When_worker_sends_a_message_for_delayed_retry : NServiceBusAcceptanceTest
    {
        static string ReceiverEndpoint => Conventions.EndpointNamingConvention(typeof(Receiver));
        static string DistributorEndpoint => Conventions.EndpointNamingConvention(typeof(Distributor));

        [Test]
        public async Task Should_also_send_a_ready_message_and_the_timeout_should_be_addressed_to_the_distributor()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Receiver>(b => b.DoNotFailOnErrorMessages())
                .WithEndpoint<Distributor>()
                .WithEndpoint<Sender>(b => b.When(c => c.WorkerSessionId != null, (s, c) =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.SetHeader("NServiceBus.Distributor.WorkerSessionId", c.WorkerSessionId);
                    return s.Send(new MyRequest(), sendOptions);
                }))
                .Done(c => c.ReceivedReadyMessage && c.DelayedRetryScheduled)
                .Run();

            Assert.IsTrue(context.ReceivedReadyMessage);
            Assert.IsTrue(context.DelayedRetryScheduled);
            Assert.AreEqual(DistributorEndpoint, context.TimeoutDestination);
        }

        public class Context : DistributorContext
        {
            public bool DelayedRetryScheduled { get; set; }
            public string TimeoutDestination { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var routing = c.UseTransport<MsmqTransport>().Routing();
                    routing.RouteToEndpoint(typeof(MyRequest), ReceiverEndpoint);
                });
            }
        }

        public class Distributor : EndpointConfigurationBuilder
        {
            public Distributor()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<TimeoutManager>();
                });
            }

            public class Detector : ReadyMessageDetector
            {
                public Detector()
                {
                    EnableByDefault();
                }
            }

            public class TimeoutManagerFake : Feature
            {
                public TimeoutManagerFake()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.AddSatelliteReceiver("TimeoutManagerFake", context.Settings.EndpointName() + ".Timeouts", TransportTransactionMode.ReceiveOnly, PushRuntimeSettings.Default,
                        OnError, OnMessage);
                }

                static Task OnMessage(IBuilder builder, MessageContext message)
                {
                    var context = builder.Build<Context>();
                    context.DelayedRetryScheduled = true;
                    context.TimeoutDestination = message.Headers["NServiceBus.Timeout.RouteExpiredTimeoutTo"];
                    return Task.CompletedTask;
                }

                static RecoverabilityAction OnError(RecoverabilityConfig arg1, ErrorContext arg2)
                {
                    return RecoverabilityAction.ImmediateRetry();
                }
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnlistWithLegacyMSMQDistributor(DistributorEndpoint, DistributorEndpoint + ".Control", 10);
                    c.Recoverability().Immediate(i => i.NumberOfRetries(0));
                    c.Recoverability().Delayed(d => d.NumberOfRetries(1));
                    c.DisableFeature<FailTestOnErrorMessageFeature>();
                });
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Task Handle(MyRequest message, IMessageHandlerContext context)
                {
                    throw new SimulatedException();
                }
            }
        }

        public class MyRequest : IMessage
        {
        }
    }
}