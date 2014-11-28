using System.Collections.Generic;
using System.Linq;

namespace NServiceBus.Persistence.FileSystem.Subscriptions
{
    using System.IO;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

    class FileSystemSubscriptionPersister : ISubscriptionStorage
    {
        private string filePath = @"z:\subscriptions";

        public void Subscribe(Address client, IEnumerable<MessageType> messageTypes)
        {
            File.AppendAllLines(filePath,
                messageTypes.Select(x => string.Format("{0}\t{1}\t{2}", client.Machine, client.Queue, x.ToString())));
        }

        public void Unsubscribe(Address client, IEnumerable<MessageType> messageTypes)
        {
            var newLines = new List<string>();
            var lines = File.ReadAllLines(filePath);
            var messageTypeStrings = messageTypes.Select(x => x.ToString()).ToArray();

            // Re-write the subscriptions file, filtering out the specified client
            // subscriptions to the specified message types
            foreach (var line in lines)
            {
                var items = line.Split(new[] { '\t' });
                if (messageTypeStrings.Any(x => x.Equals(items[2])) && client.Equals(new Address(items[1], items[0])))
                    continue;
                newLines.Add(line);
            }
            File.WriteAllLines(filePath, newLines);
        }

        public IEnumerable<Address> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            var addresses = new HashSet<Address>();
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                var items = line.Split(new[] { '\t' });
                foreach (var messageType in messageTypes)
                {
                    if (messageType.ToString().Equals(items[2]))
                    {
                        addresses.Add(new Address(items[1], items[0]));
                    }
                }
            }
            return addresses;
        }

        public void Init()
        {

        }
    }
}
