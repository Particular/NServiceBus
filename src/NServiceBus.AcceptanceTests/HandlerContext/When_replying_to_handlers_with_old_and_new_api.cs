namespace NServiceBus.AcceptanceTests.HandlerContext
{
    using System;
    using System.Collections.Generic;
    using global::NServiceBus.AcceptanceTesting;
    using global::NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_replying_to_handlers_with_old_and_new_api : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_call_old_style_first_and_then_new_style()
        {
            var context = new Context { Id = Guid.NewGuid() };

            Scenario.Define(context)
                    .WithEndpoint<Sender>(b => b.Given(bus => bus.Send(new MyCommand
                    {
                        Id = context.Id
                    })))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.HandlersExecuted.Count == 3)
                    .Run();

            Verify.AssertOldAndNewStyleHandlersAreInvoked(context);
            Verify.AssertOldStyleHandlerIsInvokedFirst(context);
            Verify.AssertNewStyleHandlerIsInvokedSecond(context);
        }

        static class Verify
        {
            public static void AssertNewStyleHandlerIsInvokedSecond(Context context)
            {
                Assert.AreEqual("NewStyle", context.HandlersExecuted[2]);
            }

            public static void AssertOldStyleHandlerIsInvokedFirst(Context context)
            {
                Assert.AreEqual("OldStyle", context.HandlersExecuted[1]);
            }

            public static void AssertOldAndNewStyleHandlersAreInvoked(Context context)
            {
                Assert.AreEqual("CommandProcessor", context.HandlersExecuted[0]);
                Assert.AreEqual(3, context.HandlersExecuted.Count, "CommandProcessor, Old and new style handlers should be invoked");
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

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<MyCommand>(typeof(Receiver));
            }


            public class OldHandlerApiHandler : IHandleMessages<MyResponse>
            {
                public Context Context { get; set; }

                public void Handle(MyResponse message)
                {
                    if (Context.Id != message.Id)
                        return;

                    Context.HandlersExecuted.Add("OldStyle");
                }
            }

            public class NewHandlerApi : IProcessResponses<MyResponse>
            {
                public Context Context { get; set; }

                public void Handle(MyResponse message, IResponseContext context)
                {
                    if (Context.Id != message.Id)
                        return;

                    Context.HandlersExecuted.Add("NewStyle");
                }
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }

            public class CommandProcessor : IProcessCommands<MyCommand>
            {
                public Context Context { get; set; }

                public void Handle(MyCommand message, ICommandContext context)
                {
                    if (Context.Id != message.Id)
                        return;

                    Context.HandlersExecuted.Add("CommandProcessor");

                    context.Reply(new MyResponse { Id = message.Id });
                }
            }
        }

        [Serializable]
        public class MyCommand : ICommand
        {
            public Guid Id { get; set; }
        }

        [Serializable]
        public class MyResponse : IResponse
        {
            public Guid Id { get; set; }
        }
    }
}