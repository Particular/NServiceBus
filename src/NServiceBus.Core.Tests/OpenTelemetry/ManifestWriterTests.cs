namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
public class ManifestWriterTests
{
    [Test]
    public async Task ShouldWriteWhenGivenCustomWriter()
    {
        var output = string.Empty;
        var testWriter = new Func<string, CancellationToken, Task>((manifestOutput, _) =>
        {
            output = manifestOutput;
            return Task.CompletedTask;
        });

        var manifest = new ManifestItems();
        manifest.Add("testOutput", new ManifestItems.ManifestItem
        {
            ArrayValue = [
            (ManifestItems.ManifestItem)"hello",
            new ManifestItems.ManifestItem() { ItemValue = [
                new("key 1", "Key 1 value"),
                new("key 2", "Key 2 value")
            ]}
            ]
        });

        var writer = new EndpointManifestWriter(testWriter, true);

        await writer.Write(manifest);

        Approver.Verify(output);
    }
}