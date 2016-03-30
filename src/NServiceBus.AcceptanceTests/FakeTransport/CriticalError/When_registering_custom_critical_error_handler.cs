namespace NServiceBus.AcceptanceTests.CriticalError
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using FakeTransport;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_registering_custom_critical_error_handler : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Critical_error_should_be_raised_inside_delegate()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithLocalCallback>(b => b.When(
                    (session, context) => session.SendLocal(new MyRequest())))
                .Done(c => c.ExceptionReceived)
                .Repeat(r => r.For(Transports.AllAvailable.SingleOrDefault(t => t.Key == "FakeTransport")))
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
                EndpointSetup<DefaultServer>(builder =>
                {
                    builder.UseTransport<FakeTransport>()
                        .RaiseCriticalErrorDuringStartup(new AggregateException("Startup task failed to complete.", new InvalidOperationException("ExceptionInBusStarts")));

                    builder.DefineCriticalErrorAction(errorContext =>
                    {
                        var aggregateException = (AggregateException) errorContext.Exception;
                        var context = builder.GetSettings().Get<Context>();
                        context.Exception = aggregateException.InnerExceptions.First();
                        context.Message = errorContext.Error;
                        context.ExceptionReceived = true;
                        return Task.FromResult(0);
                    });
                });
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Task Handle(MyRequest request, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MyRequest : IMessage
        {
        }
    }
}