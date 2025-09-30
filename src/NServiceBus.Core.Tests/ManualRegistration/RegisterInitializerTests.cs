namespace NServiceBus.Core.Tests.ManualRegistration;

using System;
using NUnit.Framework;

[TestFixture]
public class RegisterInitializerTests
{
    [Test]
    public void RegisterInitializer_Generic_Should_Store_Initializer_Type()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterInitializer<TestInitializer>();

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredInitializers initializers), Is.True);
        Assert.That(initializers.InitializerTypes, Has.Count.EqualTo(1));
        Assert.That(initializers.InitializerTypes, Does.Contain(typeof(TestInitializer)));
    }

    [Test]
    public void RegisterInitializer_NonGeneric_Should_Store_Initializer_Type()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterInitializer(typeof(TestInitializer));

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredInitializers initializers), Is.True);
        Assert.That(initializers.InitializerTypes, Has.Count.EqualTo(1));
        Assert.That(initializers.InitializerTypes, Does.Contain(typeof(TestInitializer)));
    }

    [Test]
    public void RegisterInitializer_Multiple_Should_Store_All()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterInitializer<TestInitializer>();
        config.RegisterInitializer<AnotherTestInitializer>();

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredInitializers initializers), Is.True);
        Assert.That(initializers.InitializerTypes, Has.Count.EqualTo(2));
        Assert.That(initializers.InitializerTypes, Does.Contain(typeof(TestInitializer)));
        Assert.That(initializers.InitializerTypes, Does.Contain(typeof(AnotherTestInitializer)));
    }

    [Test]
    public void RegisterInitializer_Null_InitializerType_Should_Throw()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        Assert.Throws<ArgumentNullException>(() => config.RegisterInitializer(null));
    }

    public class TestInitializer : INeedInitialization
    {
        public void Customize(EndpointConfiguration configuration)
        {
        }
    }

    public class AnotherTestInitializer : INeedInitialization
    {
        public void Customize(EndpointConfiguration configuration)
        {
        }
    }
}

