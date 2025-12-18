namespace NServiceBus.AcceptanceTests.Core.Conventions;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using Logging;
using NUnit.Framework;

public class When_receiving_unobtrusive_message_without_handler : NServiceBusAcceptanceTest
{
    [Test]
    public void Message_should_be_moved_to_error_because_handler_not_found()
    {
        Context context = null;
        Assert.That(async () =>
        {
            await Scenario.Define<Context>(ctx => context = ctx)
                .WithEndpoint<Sender>(c => c.When(s => s.Send(new MyCommand())))
                .WithEndpoint<Receiver>()
                .Run();
        }, Throws.Exception.With.InnerException.InstanceOf<InvalidOperationException>().
            And.InnerException.Message.Contains("No handlers could be found for message type: NServiceBus.AcceptanceTests.Core.Conventions.When_receiving_unobtrusive_message_without_handler+MyCommand"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Logs.Any(l => l.Level == LogLevel.Warn && l.Message.Contains($"Message header '{typeof(MyCommand).FullName}' was mapped to type '{typeof(MyCommand).FullName}' but that type was not found in the message registry, ensure the same message registration conventions are used in all endpoints, especially if using unobtrusive mode.")), Is.False, "Message type could not be mapped.");
            Assert.That(context.Logs.Any(l => l.Level == LogLevel.Warn && l.Message.Contains($"Could not determine message type from message header '{typeof(MyCommand).FullName}'")), Is.False, "Message type could not be mapped.");
        }
    }

    public class Context : ScenarioContext;

    public class Sender : EndpointConfigurationBuilder
    {
        public Sender() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.Conventions()
                    .DefiningCommandsAs(t => t.Namespace != null && t.FullName == typeof(MyCommand).FullName);
                c.ConfigureRouting().RouteToEndpoint(typeof(MyCommand), typeof(Receiver));
            });
    }


    public class Receiver : EndpointConfigurationBuilder
    {
        public Receiver() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.Conventions().DefiningCommandsAs(t => t.Namespace != null && t.FullName == typeof(MyCommand).FullName);
            });
    }

    public class MyCommand;
}