namespace NServiceBus.Core.Tests.Transport
{
    using System;
    using NUnit.Framework;
    using Transports.Msmq;

    [TestFixture]
    public class When_specifying_a_queue_name_for_msmq_with_total_length_including_machine_name_and_private_identifier_exceeding_114_characters
    {
        [Test]
        public void Should_be_replaced_by_a_deterministic_guid ()
        {
            var address = Address.Parse("WhenSpecifyingAQueuNameWithTotalLengthIncludingMachineNameAndPrivateIdentifierExceeding114Chars@MyMachine");

            var path = MsmqQueueCreator.GetFullPathWithoutPrefix(address);
            var queueName = path.Replace(@"MyMachine\private$\", "");

            var secondPath = MsmqQueueCreator.GetFullPathWithoutPrefix(address);
            
            Guid isAGuid;
            Assert.IsTrue(Guid.TryParse(queueName, out isAGuid));
            Assert.AreEqual(path, secondPath);
        }
    }
}