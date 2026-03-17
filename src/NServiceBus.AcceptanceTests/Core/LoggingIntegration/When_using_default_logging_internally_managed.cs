#nullable enable

namespace NServiceBus.AcceptanceTests.Core.LoggingIntegration;

using System;
using System.IO;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Logging;
using NUnit.Framework;

public class When_using_default_logging_internally_managed : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_write_to_rolling_file()
    {
        var logDirectory = Path.Combine(Path.GetTempPath(), "nsb-acceptance-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(logDirectory);

        try
        {
            LogManager.Use<DefaultFactory>().Directory(logDirectory);

            await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithDefaultLogging>(b =>
                {
                    b.ToCreateInstance(
                        (_, configuration) => Endpoint.Create(configuration),
                        (startableEndpoint, _, ct) => startableEndpoint.Start(ct));
                    b.When(e => e.SendLocal(new SomeMessage()));
                })
                .Done(c => c.MessageReceived)
                .Run();

            var logFiles = Directory.GetFiles(logDirectory, "nsb_log_*.txt");
            Assert.That(logFiles.Length, Is.Not.Empty, "Should have created at least one log file");

            var logContent = await File.ReadAllTextAsync(logFiles[0]);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(logContent, Does.Contain("EndpointWithDefaultLogging"));
                Assert.That(logContent, Does.Contain("INFO"));
            }
        }
        finally
        {
            if (Directory.Exists(logDirectory))
            {
                Directory.Delete(logDirectory, true);
            }
        }
    }

    public class Context : ScenarioContext
    {
        public bool MessageReceived { get; set; }
    }

    public class EndpointWithDefaultLogging : EndpointConfigurationBuilder
    {
        public EndpointWithDefaultLogging() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.RegisterComponents(s => s.AddSingleton(c.GetSettings().Get<Context>()));
            });

        [Handler]
        public class Handler(Context testContext) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SomeMessage : IMessage;
}