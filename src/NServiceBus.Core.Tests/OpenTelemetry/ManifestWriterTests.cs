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

        var manifest = new ManifestItem()
        {
            ArrayValue = [
            new ManifestItem() { StringValue = "hello" },
            new ManifestItem() { ItemValue = [
                new("key 1", new ManifestItem { StringValue = "Key 1 value" }),
                new("key 2", new ManifestItem { StringValue = "Key 2 value" })
            ]}
            ]
        };

        var writer = new EndpointManifestWriter(testWriter, true);

        await writer.Write(manifest);

        Approver.Verify(output);
    }
}