namespace NServiceBus.Core.Tests.Config
{
    using System;
    using System.Diagnostics;
    using NUnit.Framework;

    [TestFixture]
    public class When_calling_Configure_With_from_IConfigureThisEndpoint
    {
        [Test]
        public void Should_Throw()
        {
            if (Debugger.IsAttached)
            {
                Assert.Throws<InvalidOperationException>(() => new Foo().Customize(null));
            }
        }
        public class Foo : IConfigureThisEndpoint
        {
            public void Customize(BusConfiguration configuration)
            {   
                configuration.TypesToScan(new Type[]{});

                Bus.Create(configuration);
            }
        }
    }
}