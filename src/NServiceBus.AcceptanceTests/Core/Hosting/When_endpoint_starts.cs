namespace NServiceBus.AcceptanceTests.Core.Hosting;

using System.IO;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_endpoint_starts : NServiceBusAcceptanceTest
{
    static string basePath = Path.Combine(TestContext.CurrentContext.TestDirectory, TestContext.CurrentContext.Test.ID);

    [Test]
    public async Task Should_emit_config_diagnostics()
    {
        // TestContext.CurrentContext.Test.ID is stable across test runs,
        // therefore we need to clear existing diagnostics file to avoid asserting on a stale file
        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, true);
        }

        await Scenario.Define<Context>()
            .WithEndpoint<MyDiagnosticsEndpoint>()
            .Done(c => c.EndpointsStarted)
            .Run();

        var endpointName = Conventions.EndpointNamingConvention(typeof(MyDiagnosticsEndpoint));
        var startupDiagnoticsFileName = $"{endpointName}-configuration.txt";

        var pathToFile = Path.Combine(basePath, startupDiagnoticsFileName);
        Assert.That(File.Exists(pathToFile), Is.True);

        await TestContext.Out.WriteLineAsync(await File.ReadAllTextAsync(pathToFile));
    }

    [Test]
    public async Task Should_write_manifest_if_enabled()
    {
        // TestContext.CurrentContext.Test.ID is stable across test runs,
        // therefore we need to clear existing manifest file to avoid asserting on a stale file
        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, true);
        }

        await Scenario.Define<Context>()
            .WithEndpoint<MyManifestEndpoint>()
            .Done(c => c.EndpointsStarted)
            .Run();

        var endpointName = Conventions.EndpointNamingConvention(typeof(MyManifestEndpoint));
        var manifestFileName = $"{endpointName}-manifest.txt";

        var pathToFile = Path.Combine(basePath, manifestFileName);
        Assert.That(File.Exists(pathToFile), Is.True);

        await TestContext.Out.WriteLineAsync(await File.ReadAllTextAsync(pathToFile));
    }


    [Test]
    public async Task Should_not_write_manifest_if_not_enabled()
    {
        // TestContext.CurrentContext.Test.ID is stable across test runs,
        // therefore we need to clear existing manifest file to avoid asserting on a stale file
        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, true);
        }

        await Scenario.Define<Context>()
            .WithEndpoint<MyEndpoint>()
            .Done(c => c.EndpointsStarted)
            .Run();

        var endpointName = Conventions.EndpointNamingConvention(typeof(MyEndpoint));
        var manifestFileName = $"{endpointName}-manifest.txt";

        var pathToFile = Path.Combine(basePath, manifestFileName);
        Assert.That(!File.Exists(pathToFile), Is.True);
    }

    class Context : ScenarioContext
    {
    }

    class MyDiagnosticsEndpoint : EndpointConfigurationBuilder
    {
        public MyDiagnosticsEndpoint()
        {
            EndpointSetup<DefaultServer>(c => c.SetDiagnosticsPath(basePath))
               .EnableStartupDiagnostics();
        }
    }

    class MyManifestEndpoint : EndpointConfigurationBuilder
    {
        public MyManifestEndpoint()
        {
#pragma warning disable NSBCOREEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            EndpointSetup<DefaultServer>(c => c.EnableManifestGeneration(basePath));
#pragma warning restore NSBCOREEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        }
    }

    class MyEndpoint : EndpointConfigurationBuilder
    {
        public MyEndpoint()
        {
            EndpointSetup<DefaultServer>();
        }
    }

    class MyMessageHandler : IHandleMessages<MyMessage>
    {
        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class MyMessage : IMessage
    {
    }
}