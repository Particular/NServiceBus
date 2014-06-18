namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Features;
    using NServiceBus.Persistence;
    using NUnit.Framework;

    [TestFixture]
    public class OutboxFeatureTests
    {
        [Test]
        public void Should_throw_if_no_outbox_capable_storage_is_found()
        {
            var outbox = new Outbox();
            var config = Configure.With(c => c.TypesToScan(new List<Type>()));

            config.Settings.Set<EnabledPersistences>(new EnabledPersistences());
            var context = new FeatureConfigurationContext(config);

            Assert.Throws<Exception>(() => outbox.SetupFeature(context), "Selected persister doesn't have support for outbox storage. Please select another storage or disable the outbox feature using config.Features(f=>f.Disable<Outbox>())");
        }
    }


}