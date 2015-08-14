namespace NServiceBus.AcceptanceTests.BestPractices
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_subscribing_to_command : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw()
        {
            var context = new Context();
            
            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) =>
                    {
                        try
                        {
                            bus.Subscribe<MyCommand>();
                            return Task.FromResult(true);
                        }
                        catch (Exception ex)
                        {
                            c.Exception = ex;
                            c.GotTheException = true;
                        }
                        return Task.FromResult(true);
                    }))
                    .Done(c => c.GotTheException)
                    .AllowExceptions()
                    .Run();

            Assert.IsInstanceOf<InvalidOperationException>(context.Exception);
        }

        public class Context : ScenarioContext
        {
            public bool GotTheException { get; set; }
            public Exception Exception{ get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }
        }
        public class MyCommand : ICommand{}
    }
}
