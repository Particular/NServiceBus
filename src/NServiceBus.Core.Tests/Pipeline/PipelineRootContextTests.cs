namespace NServiceBus.Core.Tests.Pipeline;

using NUnit.Framework;
using Transport;

[TestFixture]
public class PipelineRootContextTests
{
    [Test]
    public void Header_return_scope_returns_deferred_headers_on_dispose()
    {
        var pool = new HeaderPool(maxPoolSize: 4);
        var owner = new FakeRootContext(pool);
        var headers = pool.Rent();
        headers["key"] = "value";

        using (owner.TrackHeaderReturns())
        {
            owner.DeferHeaderReturn(headers);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(headers["key"], Is.EqualTo("value"), "Deferred headers stay readable inside the scope.");
                Assert.That(pool.Count, Is.Zero, "Nothing is pooled before the scope ends.");
            }
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(headers, Is.Empty);
            Assert.That(pool.Count, Is.EqualTo(1));
        }
    }

    [Test]
    public void Header_return_scope_can_be_disposed_more_than_once()
    {
        var pool = new HeaderPool(maxPoolSize: 4);
        var owner = new FakeRootContext(pool);
        var headers = pool.Rent();

        var scope = owner.TrackHeaderReturns();
        owner.DeferHeaderReturn(headers);
        scope.Dispose();
        scope.Dispose();

        Assert.That(pool.Count, Is.EqualTo(1), "A second dispose must not pool the dictionary again.");
    }
}
