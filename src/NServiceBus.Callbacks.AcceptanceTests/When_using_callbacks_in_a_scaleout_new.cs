﻿namespace NServiceBus.AcceptanceTests.Callbacks
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Support;
    using NUnit.Framework;

    public class When_using_callbacks_in_a_scaleout_new : NServiceBusAcceptanceTest
    {
        [Test]
        public void Each_client_should_have_a_unique_input_queue()
        {
            //to avoid processing each others callbacks
            Scenario.Define(() => new Context { Id = Guid.NewGuid() })
                    .WithEndpoint<Client>(b => b.CustomConfig(c => RuntimeEnvironment.MachineNameAction = () => "ClientA")
                        .Given(async (bus, context) =>
                        {
                            await bus.RequestWithTransientlyHandledResponseAsync<MyResponse>(new MyRequest
                            {
                                Id = context.Id,
                                Client = RuntimeEnvironment.MachineName
                            }, new SendOptions())
                                .ContinueWith(t => context.CallbackAFired = true);
                        }))
                    .WithEndpoint<Client>(b => b.CustomConfig(c => RuntimeEnvironment.MachineNameAction = () => "ClientB")
                        .Given(async (bus, context) =>
                        {
                            await bus.RequestWithTransientlyHandledResponseAsync<MyResponse>(new MyRequest
                            {
                                Id = context.Id,
                                Client = RuntimeEnvironment.MachineName
                            }, new SendOptions())
                                .ContinueWith(t => context.CallbackBFired = true);
                        }))
                    .WithEndpoint<Server>()
                    .Done(c => c.ClientAGotResponse && c.ClientBGotResponse)
                    .Repeat(r => r.For<AllBrokerTransports>()
                    )
                    .Should(c =>
                        {
                            Assert.True(c.CallbackAFired, "Callback on ClientA should fire");
                            Assert.True(c.CallbackBFired, "Callback on ClientB should fire");
                            Assert.False(c.ResponseEndedUpAtTheWrongClient, "One of the responses ended up at the wrong client");
                        })
                      .Run(new RunSettings { UseSeparateAppDomains = true });
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public bool ClientAGotResponse { get; set; }

            public bool ClientBGotResponse { get; set; }

            public bool ResponseEndedUpAtTheWrongClient { get; set; }

            public bool CallbackAFired { get; set; }

            public bool CallbackBFired { get; set; }
        }

        public class Client : EndpointConfigurationBuilder
        {
            public Client()
            {
                EndpointSetup<DefaultServer>(c => c.ScaleOut().UseUniqueBrokerQueuePerMachine())
                    .AddMapping<MyRequest>(typeof(Server));
            }

            public class MyResponseHandler : IProcessResponses<MyResponse>
            {
                public Context Context { get; set; }

                public void Handle(MyResponse response, IResponseContext context)
                {
                    if (Context.Id != response.Id)
                    {
                        return;
                    }

                    if (RuntimeEnvironment.MachineName == "ClientA")
                        Context.ClientAGotResponse = true;
                    else
                    {
                        Context.ClientBGotResponse = true;
                    }

                    if (RuntimeEnvironment.MachineName != response.Client)
                    {
                        Context.ResponseEndedUpAtTheWrongClient = true;
                    }
                }
            }
        }

        public class Server : EndpointConfigurationBuilder
        {
            public Server()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IProcessCommands<MyRequest>
            {
                public Context Context { get; set; }

                public void Handle(MyRequest request, ICommandContext context)
                {
                    if (Context.Id != request.Id)
                    {
                        return;
                    }

                    context.Reply(new MyResponse { Id = request.Id, Client = request.Client });
                }
            }
        }

        [Serializable]
        public class MyRequest : IMessage
        {
            public Guid Id { get; set; }

            public string Client { get; set; }
        }

        [Serializable]
        public class MyResponse : IMessage
        {
            public Guid Id { get; set; }

            public string Client { get; set; }
        }
    }
}
