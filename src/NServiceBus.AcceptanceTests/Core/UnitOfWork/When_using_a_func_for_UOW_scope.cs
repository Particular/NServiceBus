namespace NServiceBus.AcceptanceTests.Core.UnitOfWork
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_using_a_func_for_UOW_scope : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_only_create_one_instance_of_UnitOrWorkInjected_per_message()
        {
            Func<ObjectBuilder.IResolver, ToBeInjected> func = v =>
            {
                var uow = v.Build<UnitOrWorkInjected>();
                return new ToBeInjected(uow);
            };

            // ReSharper disable once UnusedVariable
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b =>
                {
                    b.CustomConfig(c => c.RegisterComponents(r =>
                    {
                        r.ConfigureComponent<Context>(DependencyLifecycle.SingleInstance);

                        r.ConfigureComponent<UnitOrWorkInjected>(DependencyLifecycle.InstancePerUnitOfWork);
                        r.ConfigureComponent(func, DependencyLifecycle.InstancePerUnitOfWork);
                    }));

                    b.When(async (bus, c) =>
                    {
                        await bus.SendLocal(new Endpoint.MyMessage());
                        await bus.SendLocal(new Endpoint.MyMessage());
                        await bus.SendLocal(new Endpoint.MyMessage());
                    });
                })
                .Done(c => c.CalledCount >= 3)
                .Run(TimeSpan.FromSeconds(15));

            Assert.AreEqual(3, UnitOrWorkInjected.InstanceCount);
        }

        public class Context : ScenarioContext
        {
            public int CalledCount;
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class TestHandler : IHandleMessages<MyMessage>
            {
                private ToBeInjected instance;
                private Context testContext;

                public TestHandler(ToBeInjected instance, Context testContext)
                {
                    this.instance = instance;
                    this.testContext = testContext;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    Interlocked.Increment(ref testContext.CalledCount);
                    return Task.FromResult(0);
                }
            }

            public class MyMessage : ICommand
            {
            }
        }

        public class ToBeInjected
        {
            public ToBeInjected(UnitOrWorkInjected uow)
            {
            }
        }

        public class UnitOrWorkInjected
        {
            public static long InstanceCount;

            public UnitOrWorkInjected()
            {
                InstanceCount++;
            }
        }
    }
}