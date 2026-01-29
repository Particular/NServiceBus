using NServiceBus;
using NUnit.Framework;

Console.WriteLine("Starting AOT tests");

RunTest(ShouldThrowIfAssemblyScanningEnabled);
await RunTestAsync(ShouldStartEndpoint).ConfigureAwait(false);

Console.WriteLine("AOT tests complete");

void ShouldThrowIfAssemblyScanningEnabled()
{
    var endpointConfiguration = new EndpointConfiguration("AOTTests.ThrowWhenScanningIsOn");

    endpointConfiguration.UseTransport<LearningTransport>();
    endpointConfiguration.UseSerialization<SystemJsonSerializer>();

    var ex = Assert.ThrowsAsync<Exception>(async () => await Endpoint.Start(endpointConfiguration).ConfigureAwait(false));

    Assert.That(ex, Is.Not.Null);
    Assert.That(ex?.Message, Contains.Substring("Assembly scanning is not supported"));
}

async Task ShouldStartEndpoint()
{
    var endpointConfiguration = new EndpointConfiguration("AOTTests.Start");

    endpointConfiguration.AssemblyScanner().Disable = true;

    endpointConfiguration.UseTransport<LearningTransport>();
    endpointConfiguration.UseSerialization<SystemJsonSerializer>();

    var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
    await endpointInstance.Stop().ConfigureAwait(false);
}

void RunTest(Action test)
{
    Console.Write($"Running {test.Method.Name}");
    test();
    Console.WriteLine($"- success");
}

async Task RunTestAsync(Func<Task> test)
{
    Console.Write($"Running {test.Method.Name}");
    await test().ConfigureAwait(false);
    Console.WriteLine($"- success");
}