namespace NServiceBus.AcceptanceTests.CriticalError
{
    using System;
    using System.Linq;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;
    using IMessage = NServiceBus.IMessage;

    public class When_registering_a_custom_criticalErrorHandler : NServiceBusAcceptanceTest
    {
        [Test]
        public void Critical_error_should_be_raised_inside_delegate()
        {
            Scenario.Define<Context>()
                .WithEndpoint<EndpointWithLocalCallback>(b => b.Given(
                    (bus, context) => bus.SendLocal(new MyRequest())))
                .AllowExceptions(exception => true)
                .Done(c => c.ExceptionReceived)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    Assert.AreEqual("Startup task failed to complete.", c.Message);
                    Assert.AreEqual("ExceptionInBusStarts", c.Exception.Message);
                })
                .Run(new TimeSpan(1, 1, 1));
        }

        public class Context : ScenarioContext
        {
            public Exception Exception { get; set; }
            public string Message { get; set; }
            public bool ExceptionReceived { get; set; }
        }

        public class EndpointWithLocalCallback : EndpointConfigurationBuilder
        {
            public static Context Context { get; set; }
            public EndpointWithLocalCallback()
            {
                EndpointSetup<DefaultServer>(builder => builder.DefineCriticalErrorAction((s, exception) =>
                {
                    var aggregateException = (AggregateException) exception;
                    aggregateException = (AggregateException)aggregateException.InnerExceptions.First();
                    Context.Exception = aggregateException.InnerExceptions.First();
                    Context.Message = s;
                    Context.ExceptionReceived = true;
                }));
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyRequest request)
                {
                }
            }


            class AfterConfigIsComplete : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }
                public void Start()
                {
                    EndpointWithLocalCallback.Context = Context;
                    throw new Exception("ExceptionInBusStarts");
                }

                public void Stop()
                {
                }
            }
        }

        [Serializable]
        public class MyRequest : IMessage{}
    }
}
