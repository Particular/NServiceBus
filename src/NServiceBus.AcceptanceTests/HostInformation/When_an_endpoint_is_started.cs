namespace NServiceBus.AcceptanceTests.HostInformation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Unicast;

    public class When_an_endpoint_is_started : NServiceBusAcceptanceTest
    {
        [Test]
        public void Host_information_should_be_available_through_DI()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<MyEndpoint>()
                    .Done(c => c.HostId != Guid.Empty)
                    .Run();

            Console.Out.WriteLine(context.HostDisplayName);
            Console.Out.WriteLine(string.Join(Environment.NewLine,context.HostProperties.Select(kvp => string.Format("{0}:{1}", kvp.Key, kvp.Value)).ToList()));

            Assert.True(context.HostDisplayName.Contains(".exe"));
        }

        public class Context : ScenarioContext
        {
            public Guid HostId { get; set; }
            public string HostDisplayName { get; set; }

            public Dictionary<string, string> HostProperties { get; set; }
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class MyStartUpTask:IWantToRunWhenBusStartsAndStops
            {
                public UnicastBus UnicastBus { get; set; }

                public Context Context { get; set; }
                public void Start()
                {
                    Context.HostId = UnicastBus.HostInformation.HostId;
                    Context.HostDisplayName = UnicastBus.HostInformation.DisplayName;
                    Context.HostProperties = UnicastBus.HostInformation.Properties;
                }

                public void Stop()
                {
                    
                }
            }
         
        }
    }
}