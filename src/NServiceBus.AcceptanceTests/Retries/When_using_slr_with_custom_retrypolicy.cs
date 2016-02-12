namespace NServiceBus.AcceptanceTests.Retries
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Config;
    using NUnit.Framework;
    using System;
    using IMessage = NServiceBus.IMessage;

    public class When_using_slr_with_custom_retrypolicy : NServiceBusAcceptanceTest
    {

        [Test]
        public void Should_only_call_slr_retrypolicy_once()
        {
            Scenario.Define(() => new Context { Id = Guid.NewGuid(), RetryPolicy = new RetryPolicy() })
                    .WithEndpoint<SLREndpoint>(b => b.Given((bus, context) => bus.SendLocal(new MessageToBeRetried { Id = context.Id })))
                    .AllowExceptions(e => e.Message.Contains("Simulated exception"))
                    .Done(c => c.NumberOfTimesInvoked >= 1)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(context =>
                    {
                        Assert.AreEqual(1, context.RetryPolicy.MaxCalls, "The SLR RetryPolicy should only be called once");
                    })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public RetryPolicy RetryPolicy { get; set; }

            public int NumberOfTimesInvoked { get; set; }
        }

        public class RetryPolicy
        {
            public int MaxCalls { get; private set; }

            int Calls { get; set; }

            int LastRetry { get; set; }

            public TimeSpan CalculateRetryPolicy(TransportMessage message)
            {
                string retriesHeader;
                message.Headers.TryGetValue(Headers.Retries, out retriesHeader);

                var retries = string.IsNullOrWhiteSpace(retriesHeader) ? 0 : int.Parse(retriesHeader);

                if (retries != LastRetry)
                {
                    Calls = 0;
                    LastRetry = retries;
                }

                Calls++;

                if (Calls > MaxCalls)
                {
                    MaxCalls = Calls;
                }

                Console.WriteLine("CalculateRetryPolicy [" + retries + "] called: " + Calls);

                return retries > 1 ? TimeSpan.MinValue : TimeSpan.FromSeconds(1);
            }
        }

        public class SLREndpoint : EndpointConfigurationBuilder
        {
            public SLREndpoint()
            {
                EndpointSetup<DefaultServer>((bc, rd) =>
                {
                    var rp = (rd.ScenarioContext as Context).RetryPolicy;

                    bc.SecondLevelRetries().CustomRetryPolicy(msg => rp.CalculateRetryPolicy(msg));
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0; //to skip the FLR
                    });
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Context Context { get; set; }

                public void Handle(MessageToBeRetried message)
                {
                    if (message.Id != Context.Id) return; // ignore messages from previous test runs

                    Context.NumberOfTimesInvoked++;

                    throw new Exception("Simulated exception");
                }
            }
        }

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}
