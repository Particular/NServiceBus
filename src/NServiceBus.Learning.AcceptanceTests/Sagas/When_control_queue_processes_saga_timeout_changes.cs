namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Transport;
using NUnit.Framework;

// The control queue runs the main receive pipeline, so control messages get what any message gets
public class When_control_queue_processes_saga_timeout_changes : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_run_the_endpoint_behaviors_for_control_messages()
    {
        var context = await Scenario.Define<Context>(c => c.Request = ControlRequest.Cancel)
            .WithEndpoint<ServiceControlSpy>()
            .WithEndpoint<EndpointWithBehaviors>(e => e.When(s => s.SendLocal(new StartSaga { Id = Guid.NewGuid().ToString() })))
            .Done(c => c.Canceled)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            // A behavior that provides a partition key, as Azure Table and Cosmos DB persistence need, can read the saga from the headers
            Assert.That(context.SagaIdSeenByPhysicalBehavior, Is.EqualTo(context.SagaId));
            // A logical-stage behavior, such as the Azure Table persistence outbox, runs for the control message
            Assert.That(context.MessageTypeSeenByLogicalBehavior, Is.EqualTo("NServiceBus.SagaTimeoutChangeRequest"));
        }
    }

    [Test]
    public async Task Should_deserialize_control_messages_whatever_the_endpoint_serializer()
    {
        var context = await Scenario.Define<Context>(c => c.Request = ControlRequest.Cancel)
            .WithEndpoint<ServiceControlSpy>()
            .WithEndpoint<EndpointUsingXml>(e => e.When(s => s.SendLocal(new StartSaga { Id = Guid.NewGuid().ToString() })))
            .Done(c => c.Canceled)
            .Run();

        Assert.That(context.Canceled, Is.True);
    }

    [Test]
    public async Task Should_move_an_invalid_request_to_the_error_queue()
    {
        var context = await Scenario.Define<Context>(c => c.Request = ControlRequest.CancelWithPayload)
            .WithEndpoint<ServiceControlSpy>()
            .WithEndpoint<ErrorSpy>()
            .WithEndpoint<EndpointWithErrorSpy>(e => e.DoNotFailOnErrorMessages().When(s => s.SendLocal(new StartSaga { Id = Guid.NewGuid().ToString() })))
            .Done(c => c.FailedMessageHeaders is not null)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.FailedMessageHeaders["NServiceBus.FailedQ"], Does.EndWith("control"));
            Assert.That(context.FailedMessageHeaders["NServiceBus.ExceptionInfo.ExceptionType"], Is.EqualTo("NServiceBus.MessageDeserializationException"));
        }
    }

    [Test]
    public async Task Should_return_delayed_retries_to_the_control_queue()
    {
        var context = await Scenario.Define<Context>(c =>
            {
                c.Request = ControlRequest.Cancel;
                c.FailControlMessages = true;
            })
            .WithEndpoint<ServiceControlSpy>()
            .WithEndpoint<ErrorSpy>()
            .WithEndpoint<EndpointWithErrorSpy>(e => e
                .CustomConfig(c => c.Recoverability().Delayed(d => d.NumberOfRetries(1).TimeIncrease(TimeSpan.FromMilliseconds(10))))
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new StartSaga { Id = Guid.NewGuid().ToString() })))
            .Done(c => c.FailedMessageHeaders is not null)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ControlMessageAttempts, Is.EqualTo(2));
            Assert.That(context.RetriesHeaderOnLastAttempt, Is.EqualTo("1"));
            // The last attempt was the delayed retry, and it failed on the control queue, so the retry went back there
            Assert.That(context.FailedMessageHeaders["NServiceBus.FailedQ"], Does.EndWith("control"));
        }
    }

    [Test]
    public async Task Should_reject_business_messages_sent_to_the_control_queue()
    {
        var context = await Scenario.Define<Context>(c => c.Request = ControlRequest.BusinessMessage)
            .WithEndpoint<ServiceControlSpy>()
            .WithEndpoint<ErrorSpy>()
            .WithEndpoint<EndpointWithErrorSpy>(e => e.DoNotFailOnErrorMessages().When(s => s.SendLocal(new StartSaga { Id = Guid.NewGuid().ToString() })))
            .Done(c => c.FailedMessageHeaders is not null)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.FailedMessageHeaders["NServiceBus.FailedQ"], Does.EndWith("control"));
            Assert.That(context.FailedMessageHeaders["NServiceBus.ExceptionInfo.Message"], Does.Contain("only accepts control messages"));
            Assert.That(context.SagaStarts, Is.EqualTo(1));
        }
    }

    public enum ControlRequest
    {
        Cancel,
        CancelWithPayload,
        BusinessMessage
    }

    public class Context : ScenarioContext
    {
        public ControlRequest Request { get; set; }
        public bool FailControlMessages { get; set; }
        public Guid SagaId { get; set; }
        public bool Canceled { get; set; }
        public int SagaStarts { get; set; }
        public int ControlMessageAttempts { get; set; }
        public string RetriesHeaderOnLastAttempt { get; set; }
        public Guid? SagaIdSeenByPhysicalBehavior { get; set; }
        public string MessageTypeSeenByLogicalBehavior { get; set; }
        public Dictionary<string, string> FailedMessageHeaders { get; set; }
    }

    public class EndpointWithBehaviors : EndpointConfigurationBuilder
    {
        public EndpointWithBehaviors() => EndpointSetup<DefaultServer>(c =>
        {
            c.ServiceControlErrorInstanceQueue(Conventions.EndpointNamingConvention(typeof(ServiceControlSpy)));
            c.ReportSagaTimeoutsToServiceControl();
            c.Pipeline.Register<ReadSagaFromHeadersBehavior>("Reads the saga from the headers, as a partition key behavior would");
            c.Pipeline.Register<RecordLogicalMessageBehavior>("Records the logical message type");
        });

        class ReadSagaFromHeadersBehavior(Context testContext) : Behavior<IIncomingPhysicalMessageContext>
        {
            public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
            {
                if (context.Message.Headers.TryGetValue("NServiceBus.SagaTimeoutManagement.SagaId", out var sagaId))
                {
                    testContext.SagaIdSeenByPhysicalBehavior = Guid.Parse(sagaId);
                }

                return next();
            }
        }

        class RecordLogicalMessageBehavior(Context testContext) : Behavior<IIncomingLogicalMessageContext>
        {
            public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
            {
                if (context.Message.MessageType.FullName == "NServiceBus.SagaTimeoutChangeRequest")
                {
                    testContext.MessageTypeSeenByLogicalBehavior = context.Message.MessageType.FullName;
                }

                return next();
            }
        }

        [Saga]
        public class TimeoutSaga(Context testContext) : Saga<TimeoutSagaData>, IAmStartedByMessages<StartSaga>, IHandleTimeouts<SomeTimeout>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TimeoutSagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationProperty).ToMessage<StartSaga>(m => m.Id);

            public Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                testContext.SagaStarts++;
                return RequestTimeout<SomeTimeout>(context, TimeSpan.FromHours(1));
            }

            public Task Timeout(SomeTimeout state, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }

    public class EndpointUsingXml : EndpointConfigurationBuilder
    {
        public EndpointUsingXml() => EndpointSetup<DefaultServer>(c =>
        {
            c.UseSerialization<XmlSerializer>();
            c.ServiceControlErrorInstanceQueue(Conventions.EndpointNamingConvention(typeof(ServiceControlSpy)));
            c.ReportSagaTimeoutsToServiceControl();
        });

        [Saga]
        public class TimeoutSaga(Context testContext) : Saga<TimeoutSagaData>, IAmStartedByMessages<StartSaga>, IHandleTimeouts<SomeTimeout>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TimeoutSagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationProperty).ToMessage<StartSaga>(m => m.Id);

            public Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                testContext.SagaStarts++;
                return RequestTimeout<SomeTimeout>(context, TimeSpan.FromHours(1));
            }

            public Task Timeout(SomeTimeout state, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }

    public class EndpointWithErrorSpy : EndpointConfigurationBuilder
    {
        public EndpointWithErrorSpy() => EndpointSetup<DefaultServer>(c =>
        {
            c.ServiceControlErrorInstanceQueue(Conventions.EndpointNamingConvention(typeof(ServiceControlSpy)));
            c.ReportSagaTimeoutsToServiceControl();
            c.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(ErrorSpy)));
            c.Pipeline.Register<FailControlMessagesBehavior>("Fails control messages when the test asks for it");
        });

        class FailControlMessagesBehavior(Context testContext) : Behavior<IIncomingLogicalMessageContext>
        {
            public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
            {
                if (context.Message.MessageType.FullName == "NServiceBus.SagaTimeoutChangeRequest")
                {
                    testContext.ControlMessageAttempts++;
                    testContext.RetriesHeaderOnLastAttempt = context.MessageHeaders.GetValueOrDefault("NServiceBus.Retries");
                    if (testContext.FailControlMessages)
                    {
                        throw new SimulatedException("Failing the control message");
                    }
                }

                return next();
            }
        }

        [Saga]
        public class TimeoutSaga(Context testContext) : Saga<TimeoutSagaData>, IAmStartedByMessages<StartSaga>, IHandleTimeouts<SomeTimeout>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TimeoutSagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationProperty).ToMessage<StartSaga>(m => m.Id);

            public Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                testContext.SagaStarts++;
                return RequestTimeout<SomeTimeout>(context, TimeSpan.FromHours(1));
            }

            public Task Timeout(SomeTimeout state, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }

    public class ErrorSpy : EndpointConfigurationBuilder
    {
        public ErrorSpy() => EndpointSetup<DefaultServer>(c =>
            c.Pipeline.Register<RecordFailedMessageBehavior>("Records failed messages"));

        class RecordFailedMessageBehavior(Context testContext) : Behavior<IIncomingPhysicalMessageContext>
        {
            public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
            {
                testContext.FailedMessageHeaders = new Dictionary<string, string>(context.Message.Headers);
                return Task.CompletedTask;
            }
        }
    }

    // Plays the part of ServiceControl: sends a control message when the endpoint reports the requested timeout
    public class ServiceControlSpy : EndpointConfigurationBuilder
    {
        public ServiceControlSpy() => EndpointSetup<DefaultServer>(c =>
            c.Pipeline.Register<RequestChangesBehavior>("Requests timeout changes"));

        class RequestChangesBehavior(Context testContext) : Behavior<IIncomingPhysicalMessageContext>
        {
            public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
            {
                var messageType = context.Message.Headers[Headers.EnclosedMessageTypes].Split('.').Last();
                using var document = JsonDocument.Parse(context.Message.Body);
                var body = document.RootElement;
                var dispatcher = context.Builder.GetRequiredService<IMessageDispatcher>();

                if (messageType == "SagaTimeoutCanceled")
                {
                    testContext.Canceled = true;
                    return;
                }

                if (messageType != "SagaTimeoutRequested")
                {
                    return;
                }

                testContext.SagaId = body.GetProperty("SagaId").GetGuid();

                if (testContext.Request == ControlRequest.BusinessMessage)
                {
                    var headers = new Dictionary<string, string>
                    {
                        [Headers.ContentType] = ContentTypes.Json,
                        [Headers.EnclosedMessageTypes] = typeof(StartSaga).FullName
                    };
                    var message = new OutgoingMessage(Guid.NewGuid().ToString(), headers, Encoding.UTF8.GetBytes($$"""{"Id":"{{Guid.NewGuid()}}"}"""));
                    await dispatcher.Dispatch(new TransportOperations(new TransportOperation(message, new UnicastAddressTag(body.GetProperty("ManagementAddress").GetString()))), new TransportTransaction());
                    return;
                }

                await When_operator_changes_saga_timeout.SendControlMessage(dispatcher, body, new Dictionary<string, object>
                {
                    ["OperationId"] = Guid.NewGuid(),
                    ["SagaType"] = body.GetProperty("SagaType").GetString(),
                    ["SagaId"] = body.GetProperty("SagaId").GetGuid(),
                    ["TimeoutId"] = body.GetProperty("TimeoutId").GetString(),
                    ["ExpectedVersion"] = body.GetProperty("Version").GetInt32(),
                    ["Change"] = "Cancel",
                    ["Payload"] = testContext.Request == ControlRequest.CancelWithPayload ? Encoding.UTF8.GetBytes("{}") : null,
                    ["PayloadContentType"] = testContext.Request == ControlRequest.CancelWithPayload ? "application/json" : null
                });
            }
        }
    }

    public class TimeoutSagaData : ContainSagaData
    {
        public virtual string CorrelationProperty { get; set; }
    }

    public class StartSaga : ICommand
    {
        public string Id { get; set; }
    }

    public class SomeTimeout;
}
