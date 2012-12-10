using System;
using V2.Messages;
using NServiceBus;

namespace V2Subscriber
{
    public class SomethingHappenedHandler : IHandleMessages<ISomethingHappened>
    {
        public void Handle(ISomethingHappened message)
        {
            Console.WriteLine("======================================================================");

            Console.WriteLine("Something happened with some data {0} and more info {1}", message.SomeData, message.MoreInfo);
        }
    }
}
