namespace NServiceBus.Persistence.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class ThrowIfRequiredPropertiesAreMissing
    {
        [Test]
        public void Should_throw_if_minimum_properties_not_set()
        {
            Assert.Throws<InvalidOperationException>(() => ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(new Dictionary<string, string>()));
        }

        [Test]
        public void Should_not_throw_if_minimum_properties_are_set()
        {
            Assert.DoesNotThrow(() => ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(new Dictionary<string, string>
                                                                                                  {
                                                                                                      {"dialect", "Boo"},
                                                                                                      {"connection.connection_string", "asdad"}
                                                                                                  }));
        }
    }
}