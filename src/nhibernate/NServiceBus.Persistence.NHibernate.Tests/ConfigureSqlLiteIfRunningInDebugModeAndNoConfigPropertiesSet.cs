namespace NServiceBus.Persistence.NHibernate.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class ConfigureSqlLiteIfRunningInDebugModeAndNoConfigPropertiesSet
    {
        [Test]
        public void Should_display_no_warning_and_not_configure_properties_if_debugger_is_not_attached_and_properties_not_set()
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Assert.Ignore("Can't run this unit test with debugger attached.");
                return;
            }

            var properties = new Dictionary<string, string>();
            ConfigureNHibernate.ConfigureSqlLiteIfRunningInDebugModeAndNoConfigPropertiesSet(properties);

            Assert.AreEqual(0, properties.Count);
        }

        [Test]
        public void Should_display_warning_and_configure_properties_if_debugger_is_attached_and_properties_not_set()
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                Assert.Ignore("Debugger needs to be attached to run this unit test.");
                return;
            }

            var properties = new Dictionary<string, string>();
            ConfigureNHibernate.ConfigureSqlLiteIfRunningInDebugModeAndNoConfigPropertiesSet(properties);

            Assert.IsTrue(properties.ContainsKey("dialect"));
            Assert.AreEqual("NHibernate.Dialect.SQLiteDialect", properties["dialect"]);
        }
    }
}
