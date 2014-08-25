namespace NServiceBus.Unicast.Tests.Helpers
{
    using System.Collections.Generic;
    using System.Linq;
    using Subscriptions;
    using Subscriptions.MessageDrivenSubscriptions;

    public class FakeSubscriptionStorage : ISubscriptionStorage
    {

        public void Subscribe(string address, IEnumerable<MessageType> messageTypes)
        {
            messageTypes.ToList().ForEach(messageType =>
                                              {
                                                  if (!storage.ContainsKey(messageType))
                                                      storage[messageType] = new List<string>();

                                                  if (!storage[messageType].Contains(address))
                                                      storage[messageType].Add(address);
                                              });
        }

        public void Unsubscribe(string address, IEnumerable<MessageType> messageTypes)
        {
            messageTypes.ToList().ForEach(messageType =>
                                              {
                                                  if (storage.ContainsKey(messageType))
                                                      storage[messageType].Remove(address);
                                              });
        }


        public IEnumerable<string> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            var result = new List<string>();
            messageTypes.ToList().ForEach(m =>
                                              {
                                                  if (storage.ContainsKey(m))
                                                      result.AddRange(storage[m]);
                                              });

            return result;
        }
        public void FakeSubscribe<T>(string address)
        {
            ((ISubscriptionStorage)this).Subscribe(address, new[] { new MessageType(typeof(T)) });
        }

        public void Init()
        {
        }

        readonly Dictionary<MessageType, List<string>> storage = new Dictionary<MessageType, List<string>>();
    }
}