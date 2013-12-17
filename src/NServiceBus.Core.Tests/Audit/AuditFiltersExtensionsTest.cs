namespace NServiceBus.Core.Tests.Audit
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Audit;
    using NUnit.Framework;

    [TestFixture]
    public class AuditFiltersExtensionsTest
    {
        private AuditFilters auditFilters;

        [SetUp]
        public void SetUp()
        {
            auditFilters = new AuditFilters();
        }

        [Test]
        public void When_enclosed_message_type_matches_filtered_type_then_filter_returns_true()
        {
            auditFilters.ExcludeMessageTypeFromAudit(typeof(SampleMessage));

            var headers = new Dictionary<string, string>
            {
                { Headers.EnclosedMessageTypes, typeof(SampleMessage).FullName }
            };
            var transportMessage = new TransportMessage("id", headers);

            var filter = auditFilters.Filters.Single();
            Assert.IsTrue(filter(transportMessage));
        }

        [Test]
        public void When_enclosed_message_type_does_not_match_filtered_type_then_filter_returns_false()
        {
            auditFilters.ExcludeMessageTypeFromAudit(typeof(SampleMessage));

            var headers = new Dictionary<string, string>
            {
                { Headers.EnclosedMessageTypes, typeof(string).FullName }
            };
            var transportMessage = new TransportMessage("id", headers);

            var filter = auditFilters.Filters.Single();
            Assert.IsFalse(filter(transportMessage));
        }

        class SampleMessage
        {
             
        }
    }
}