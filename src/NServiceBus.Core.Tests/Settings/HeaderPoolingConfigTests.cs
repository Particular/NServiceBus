#nullable enable

namespace NServiceBus.Settings;

using System;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class HeaderPoolingConfigTests
{
    [Test]
    public void Default_pool_is_always_allocate()
    {
        var endpointConfiguration = new EndpointConfiguration("HeaderPoolingDisabledByDefault");

        Assert.That(endpointConfiguration.Settings.GetHeaderPool(), Is.SameAs(HeaderPool.AlwaysAllocate));
    }

    [Test]
    public void Enable_uses_the_shared_pool()
    {
        var endpointConfiguration = new EndpointConfiguration("HeaderPoolingEnabled");

        endpointConfiguration.EnableHeaderPooling();

        Assert.That(endpointConfiguration.Settings.GetHeaderPool(), Is.SameAs(HeaderPool.Shared));
    }

    [Test]
    public void Enable_then_disable_is_last_write_wins()
    {
        var endpointConfiguration = new EndpointConfiguration("HeaderPoolingLastWriteWins");

        endpointConfiguration.EnableHeaderPooling();
        Assert.That(endpointConfiguration.Settings.GetHeaderPool(), Is.SameAs(HeaderPool.Shared));

        endpointConfiguration.DisableHeaderPooling();
        Assert.That(endpointConfiguration.Settings.GetHeaderPool(), Is.SameAs(HeaderPool.AlwaysAllocate));
    }

    [Test]
    public void Enable_with_a_pool_uses_that_pool()
    {
        var endpointConfiguration = new EndpointConfiguration("HeaderPoolingCustomPool");
        var pool = new HeaderPool(maxPoolSize: 512, maxRetainedCapacityPerItem: 128);

        endpointConfiguration.EnableHeaderPooling(pool);

        Assert.That(endpointConfiguration.Settings.GetHeaderPool(), Is.SameAs(pool));
    }

    [Test]
    public void Enable_with_null_pool_throws()
    {
        var endpointConfiguration = new EndpointConfiguration("HeaderPoolingNullPool");

        Assert.Throws<ArgumentNullException>(() => endpointConfiguration.EnableHeaderPooling(null!));
    }

    [Test]
    public void Disable_alone_sets_always_allocate()
    {
        var endpointConfiguration = new EndpointConfiguration("HeaderPoolingDisableAlone");

        endpointConfiguration.DisableHeaderPooling();

        Assert.That(endpointConfiguration.Settings.GetHeaderPool(), Is.SameAs(HeaderPool.AlwaysAllocate));
    }
}