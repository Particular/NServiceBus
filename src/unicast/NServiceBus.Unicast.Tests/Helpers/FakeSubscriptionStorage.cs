namespace NServiceBus.Unicast.Tests.Helpers
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Unicast.Subscriptions;

    public class FakeSubscriptionStorage : ISubscriptionStorage
    {

        void ISubscriptionStorage.Subscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            messageTypes.ToList().ForEach(messageType =>
                                              {
                                                  if (!storage.ContainsKey(messageType))
                                                      storage[messageType] = new List<Address>();

                                                  if (!storage[messageType].Contains(address))
                                                      storage[messageType].Add(address);
                                              });
        }

        void ISubscriptionStorage.Unsubscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            messageTypes.ToList().ForEach(messageType =>
                                              {
                                                  if (storage.ContainsKey(messageType))
                                                      storage[messageType].Remove(address);
                                              });
        }


        IEnumerable<Address> ISubscriptionStorage.GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            var result = new List<Address>();
            messageTypes.ToList().ForEach(m =>
                                              {
                                                  if (storage.ContainsKey(m))
                                                      result.AddRange(storage[m]);
                                              });

            return result;
        }
        public void FakeSubscribe<T>(Address address)
        {
            ((ISubscriptionStorage)this).Subscribe(address, new[] { new MessageType(typeof(T)) });
        }

        public void Init()
        {
        }

        readonly Dictionary<MessageType, List<Address>> storage = new Dictionary<MessageType, List<Address>>();
    }
}