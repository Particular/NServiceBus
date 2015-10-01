namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_distributing_a_command : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_round_robin()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When((bus, c) => bus.SendAsync(new Request())))
                .WithEndpoint<Receiver1>()
                .WithEndpoint<Receiver2>()
                .Done(c => c.Receiver1TimesCalled > 4 && c.Receiver2TimesCalled > 4)
                .Run();

            Assert.IsTrue(context.Receiver1TimesCalled > 4);
            Assert.IsTrue(context.Receiver2TimesCalled > 4);
        }

        public class Context : ScenarioContext
        {
            public int Receiver1TimesCalled { get; set; }
            public int Receiver2TimesCalled { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                var basePath = AppDomain.CurrentDomain.BaseDirectory;

                File.WriteAllLines(Path.Combine(basePath, "DistributingACommand.Receiver.txt"), new[]
                {
                    "DistributingACommand.Receiver-1",
                    "DistributingACommand.Receiver-2"
                });

                EndpointSetup<DefaultServer>(c => c.UseDynamicRouting<FileBasedRoundRobinDistribution>()
                    .LookForFilesIn(basePath))
                    .WithConfig<UnicastBusConfig>(c => c.MessageEndpointMappings.Add(new MessageEndpointMapping
                    {
                        AssemblyName = typeof(Request).Assembly.FullName,
                        Endpoint = "DistributingACommand.Receiver"
                    }));
            }

            public class ResponseHandler : IHandleMessages<Response>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public Task Handle(Response message)
                {
                    switch (message.EndpointName)
                    {
                        case "Receiver1":
                            Context.Receiver1TimesCalled++;
                            break;
                        case "Receiver2":
                            Context.Receiver2TimesCalled++;
                            break;
                    }

                    return Bus.SendAsync(new Request());
                }
            }
        }

        public class Receiver1 : EndpointConfigurationBuilder
        {
            public Receiver1()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EndpointName("DistributingACommand.Receiver");
                    c.UseCustomLogicalToTransportAddressTranslation((address, @default) => "DistributingACommand.Receiver-1");
                });
            }

            public class MyMessageHandler : IHandleMessages<Request>
            {
                public IBus Bus { get; set; }

                public Task Handle(Request message)
                {
                    return Bus.ReplyAsync(new Response
                    {
                        EndpointName = "Receiver1"
                    });
                }
            }
        }

        public class Receiver2 : EndpointConfigurationBuilder
        {
            public Receiver2()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EndpointName("DistributingACommand.Receiver");
                    c.UseCustomLogicalToTransportAddressTranslation((address, @default) => "DistributingACommand.Receiver-2");
                });
            }

            public class MyMessageHandler : IHandleMessages<Request>
            {
                public IBus Bus { get; set; }

                public Task Handle(Request message)
                {
                    return Bus.ReplyAsync(new Response
                    {
                        EndpointName = "Receiver2"
                    });
                }
            }
        }

        [Serializable]
        public class Request : ICommand
        {
        }

        [Serializable]
        public class Response : IMessage
        {
            public string EndpointName { get; set; }
        }

    }
}
