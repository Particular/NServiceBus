using System;
using V2.Messages;
using NServiceBus;

namespace V2Subscriber
{
    public class SomethingHappenedHandler : IHandleMessages<SomethingHappened>
    {
        public void Handle(SomethingHappened message)
        {
            Console.WriteLine("Something happened with some data {0} and more info {1}", message.SomeData, message.MoreInfo);
        }
    }
}
