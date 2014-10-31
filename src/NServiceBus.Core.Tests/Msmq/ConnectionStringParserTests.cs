namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using NServiceBus.Config;
    using NUnit.Framework;

    [TestFixture]
    public class ConnectionStringParserTests
    {
        [Test]
        public void Should_correctly_parse_full_connection_string()
        {
            const string connectionString = "deadLetter=true;journal=true;cacheSendConnection=true;useTransactionalQueues=true;timeToReachQueue=00:00:30";
            var parser = new MsmqConnectionStringBuilder(connectionString);
            var settings = parser.RetrieveSettings();

            Assert.AreEqual(true, settings.UseDeadLetterQueue);
            Assert.AreEqual(true, settings.UseJournalQueue);
            Assert.AreEqual(true, settings.UseConnectionCache);
            Assert.AreEqual(true, settings.UseTransactionalQueues);
            Assert.AreEqual(TimeSpan.FromSeconds(30), settings.TimeToReachQueue);
        }

        [Test]
        public void Should_correctly_parse_full_connection_string_with_false()
        {
            const string connectionString = "deadLetter=false;journal=false;cacheSendConnection=false;useTransactionalQueues=false;timeToReachQueue=00:00:30";
            var parser = new MsmqConnectionStringBuilder(connectionString);
            var settings = parser.RetrieveSettings();

            Assert.AreEqual(false, settings.UseDeadLetterQueue);
            Assert.AreEqual(false, settings.UseJournalQueue);
            Assert.AreEqual(false, settings.UseConnectionCache);
            Assert.AreEqual(false, settings.UseTransactionalQueues);
            Assert.AreEqual(TimeSpan.FromSeconds(30), settings.TimeToReachQueue);
        }
    }
}