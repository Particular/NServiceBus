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
                                                                                                      {"connection.connection_string", "aString"}
                                                                                                  }));
        }

        [Test]
        public void Should_not_throw_if_connectionStringName_is_used_instead_of_connectionString()
        {
            Assert.DoesNotThrow(() => ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(new Dictionary<string, string>
                                                                                                  {
                                                                                                      {"connection.connection_string_name", "aString"}
                                                                                                  }));
        }
    }
}