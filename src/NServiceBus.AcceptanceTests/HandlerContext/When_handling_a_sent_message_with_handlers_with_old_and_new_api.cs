namespace NServiceBus.AcceptanceTests.HandlerContext
{
    using System;
    using System.Collections.Generic;
    using global::NServiceBus.AcceptanceTesting;
    using global::NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_handling_a_sent_message_with_handlers_with_old_and_new_api : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_call_old_style_first_and_then_new_style()
        {
            var context = new Context { Id = Guid.NewGuid() };

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new MyMessage
                    {
                        Id = context.Id
                    })))
                    .Done(c => c.HandlersExecuted.Count == 2)
                    .Run();

            Verify.AssertOldAndNewStyleHandlersAreInvoked(context);
            Verify.AssertOldStyleHandlerIsInvokedFirst(context);
            Verify.AssertNewStyleHandlerIsInvokedSecond(context);
        }

        static class Verify
        {
            public static void AssertNewStyleHandlerIsInvokedSecond(Context context)
            {
                Assert.AreEqual("NewStyle", context.HandlersExecuted[1]);
            }

            public static void AssertOldStyleHandlerIsInvokedFirst(Context context)
            {
                Assert.AreEqual("OldStyle", context.HandlersExecuted[0]);
            }

            public static void AssertOldAndNewStyleHandlersAreInvoked(Context context)
            {
                Assert.AreEqual(2, context.HandlersExecuted.Count, "Old and new style handlers should be invoked");
            }
        }

        public class Context : ScenarioContext
        {
            public Context()
            {
                HandlersExecuted = new List<string>();
            }

            public List<string> HandlersExecuted { get; private set; }
            public Guid Id { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<MyMessage>(typeof(Endpoint));
            }
        }

        [Serializable]
        public class MyMessage : IMessage
        {
            public Guid Id { get; set; }
        }

        public class OldHandlerApiHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public void Handle(MyMessage message)
            {
                if (Context.Id != message.Id)
                    return;

                Context.HandlersExecuted.Add("OldStyle");
            }
        }

        public class NewStyleHandlerApiHandler : IHandle<MyMessage>
        {
            // TODO: Could we maybe also leverage the context object to pass in that dependency?
            public Context Context { get; set; }

            public void Handle(MyMessage message, IHandleContext context)
            {
                if (Context.Id != message.Id)
                    return;

                Context.HandlersExecuted.Add("NewStyle");
            }
        }

    }
}