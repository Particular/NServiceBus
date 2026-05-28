namespace NServiceBus.Core.Tests.StaticHeaders;

using System.Threading.Tasks;
using NUnit.Framework;
using Testing;

[TestFixture]
public class ApplyStaticHeadersBehaviorTests
{
    [Test]
    public async Task Should_copy_all_configured_static_headers_to_outgoing_headers()
    {
        var staticHeaders = new CurrentStaticHeaders
        {
            ["header-a"] = "value-a",
            ["header-b"] = "value-b"
        };
        var behavior = new ApplyStaticHeadersBehavior(staticHeaders);
        var context = new TestableOutgoingLogicalMessageContext();

        await behavior.Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers["header-a"], Is.EqualTo("value-a"));
            Assert.That(context.Headers["header-b"], Is.EqualTo("value-b"));
        }
    }

    [Test]
    public async Task Should_call_next_and_leave_headers_unchanged_when_no_static_headers_are_configured()
    {
        var behavior = new ApplyStaticHeadersBehavior([]);
        var context = new TestableOutgoingLogicalMessageContext();
        context.Headers["existing-header"] = "existing-value";

        await behavior.Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers["existing-header"], Is.EqualTo("existing-value"));
            Assert.That(context.Headers.ContainsKey("header-a"), Is.False);
        }
    }

    [Test]
    public async Task Should_overwrite_existing_outgoing_header_when_static_header_has_same_key()
    {
        var staticHeaders = new CurrentStaticHeaders
        {
            ["shared-key"] = "static-value"
        };
        var behavior = new ApplyStaticHeadersBehavior(staticHeaders);
        var context = new TestableOutgoingLogicalMessageContext();
        context.Headers["shared-key"] = "existing-value";

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers["shared-key"], Is.EqualTo("static-value"));
    }
}
