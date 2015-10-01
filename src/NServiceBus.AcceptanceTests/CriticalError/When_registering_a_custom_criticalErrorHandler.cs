namespace NServiceBus.AcceptanceTests.CriticalError
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.FakeTransport;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NUnit.Framework;
    using ScenarioDescriptors;
    using IMessage = NServiceBus.IMessage;

    public class When_registering_a_custom_criticalErrorHandler : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Critical_error_should_be_raised_inside_delegate()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithLocalCallback>(b => b.When(
                    (bus, context) => bus.SendLocalAsync(new MyRequest())))
                .AllowExceptions()
                .Done(c => c.ExceptionReceived)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    Assert.AreEqual("Startup task failed to complete.", c.Message);
                    Assert.AreEqual("ExceptionInBusStarts", c.Exception.Message);
                })
                .Run();
        }

        public class Context : FakeTransportContext
        {
            public Exception Exception { get; set; }
            public string Message { get; set; }
            public bool ExceptionReceived { get; set; }
        }

        public class EndpointWithLocalCallback : EndpointConfigurationBuilder
        {
            public EndpointWithLocalCallback()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                    builder.UseTransport<FakeTransport>()
                        .RaiseCriticalErrorDuringStartup(new AggregateException("Startup task failed to complete.", new InvalidOperationException("ExceptionInBusStarts")));

                    builder.DefineCriticalErrorAction((s, exception) =>
                    {
                        var aggregateException = (AggregateException) exception;
                        var context = builder.GetSettings().Get<Context>();
                        context.Exception = aggregateException.InnerExceptions.First();
                        context.Message = s;
                        context.ExceptionReceived = true;
                    });
                }).IncludeType<FakeTransportConfigurator>();
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Task Handle(MyRequest request)
                {
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MyRequest : IMessage{}
    }
}
