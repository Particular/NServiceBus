
namespace NServiceBus.AcceptanceTests.PipelineExt
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Audit;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    /// <summary>
    /// This is a demo on how pipeline overrides can be used to control which messages that gets audited by NServiceBus
    /// </summary>
    public class FilteringWhatGetsAudited : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task RunDemo()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<UserEndpoint>(b => b.When(bus => bus.SendLocal(new MessageToBeAudited())))
                .WithEndpoint<AuditSpy>()
                .Done(c => c.Done)
                .Run();

            Assert.IsFalse(context.WrongMessageAudited);
        }


        public class UserEndpoint : EndpointConfigurationBuilder
        {
            public UserEndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .AuditTo<AuditSpy>();
            }

            class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new Message3());
                }
            }

            class Message3Handler : IHandleMessages<Message3>
            {
                public Task Handle(Message3 message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }

            class AddContextStorage : Behavior<IIncomingPhysicalMessageContext>
            {
                public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
                {
                    context.Extensions.Set(new AuditFilterResult());

                    return next();
                }

                public class Registration : RegisterStep
                {
                    public Registration()
                        : base("AddContextStorage", typeof(AddContextStorage), "Adds state to the context so that downstream behaviors can turn audit offf")
                    {
                        InsertBefore(WellKnownStep.AuditProcessedMessage);
                    }
                }
            }

            class SetFiltering : Behavior<IIncomingLogicalMessageContext>
            {
                public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
                {
                    if (context.Message.MessageType == typeof(MessageToBeAudited))
                    {
                        context.Extensions.Get<AuditFilterResult>().DoNotAuditMessage = true;
                    }

                    return next();
                }
            }

            class AuditFilterResult
            {
                public bool DoNotAuditMessage { get; set; }
            }

            class FilteringAuditBehavior : Behavior<IAuditContext>
            {
                public override Task Invoke(IAuditContext context, Func<Task> next)
                {
                    AuditFilterResult result;

                    if (context.Extensions.TryGet(out result) && result.DoNotAuditMessage)
                    {
                        return Task.FromResult(0);
                    }
                    return next();
                }

                public class Registration : RegisterStep
                {
                    public Registration()
                        : base("FilteringAudit", typeof(FilteringAuditBehavior), "Prevents audits if needed")
                    {
                    }
                }
            }

            class AuditFilteringOverride : INeedInitialization
            {
                public void Customize(BusConfiguration configuration)
                {
                    configuration.Pipeline.Register<AddContextStorage.Registration>();
                    configuration.Pipeline.Register("SetFiltering", typeof(SetFiltering), "Filters audit entries");
                    configuration.Pipeline.Register<FilteringAuditBehavior.Registration>();
                }
            }
        }

        public class AuditSpy : EndpointConfigurationBuilder
        {
            public AuditSpy()
            {
                EndpointSetup<DefaultServer>();
            }

            class AuditMessageHandler : IHandleMessages<MessageToBeAudited>
            {
                public Context MyContext { get; set; }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    MyContext.WrongMessageAudited = true;
                    return Task.FromResult(0);
                }
            }

            class Message3Handler : IHandleMessages<Message3>
            {
                public Context MyContext { get; set; }

                public Task Handle(Message3 message, IMessageHandlerContext context)
                {
                    MyContext.Done = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public bool WrongMessageAudited { get; set; }
        }


        [Serializable]
        public class MessageToBeAudited : IMessage
        {
        }

        [Serializable]
        public class Message3 : IMessage
        {
        }
    }
}
