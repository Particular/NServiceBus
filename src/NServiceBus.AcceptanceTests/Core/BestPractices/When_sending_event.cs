﻿namespace NServiceBus.AcceptanceTests.Core.BestPractices
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_event : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_throw()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(async (session, c) =>
                {
                    try
                    {
                        await session.SendLocal(new MyEvent());
                    }
                    catch (Exception ex)
                    {
                        c.Exception = ex;
                        c.GotTheException = true;
                    }
                }))
                .Done(c => c.GotTheException)
                .Run();

            Assert.IsInstanceOf<Exception>(context.Exception);
        }

        public class Context : ScenarioContext
        {
            public bool GotTheException { get; set; }
            public Exception Exception { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}