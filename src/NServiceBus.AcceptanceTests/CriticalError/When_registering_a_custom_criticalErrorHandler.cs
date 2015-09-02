namespace NServiceBus.AcceptanceTests.CriticalError
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
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
                .WithEndpoint<EndpointWithLocalCallback>(b => b.Given(
                    (bus, context) =>
                    {
                        bus.SendLocal(new MyRequest());
                        return Task.FromResult(0);
                    }))
                .AllowExceptions(exception => true)
                .Done(c => c.ExceptionReceived)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    Assert.AreEqual("Startup task failed to complete.", c.Message);
                    Assert.AreEqual("ExceptionInBusStarts", c.Exception.Message);
                })
                .Run();
        }

        public class Context : ScenarioContext
        {
            public Exception Exception { get; set; }
            public string Message { get; set; }
            public bool ExceptionReceived { get; set; }
        }

        public class EndpointWithLocalCallback : EndpointConfigurationBuilder
        {
            public EndpointWithLocalCallback()
            {
                EndpointSetup<DefaultServer>(builder => builder.DefineCriticalErrorAction((s, exception) =>
                {
                    var aggregateException = (AggregateException) exception;
                    aggregateException = (AggregateException)aggregateException.InnerExceptions.First();

                    var context = builder.GetSettings().Get<Context>();
                    context.Exception = aggregateException.InnerExceptions.First();
                    context.Message = s;
                    context.ExceptionReceived = true;
                }));
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Task Handle(MyRequest request)
                {
                    return Task.FromResult(0);
                }
            }

            class AfterConfigIsComplete : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }
                public Task StartAsync()
                {
                    throw new Exception("ExceptionInBusStarts");
                }

                public Task StopAsync()
                {
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MyRequest : IMessage{}
    }
}
