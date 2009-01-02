using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using V2.Messages;
using NServiceBus;

namespace V2Subscriber
{
    public class SomethingHappenedHandler : IMessageHandler<SomethingHappened>
    {
        #region IMessageHandler<SomethingHappened> Members

        public void Handle(SomethingHappened message)
        {
            Console.WriteLine("Something happened with some data {0} and more info {1}", message.SomeData, message.MoreInfo);
        }

        #endregion
    }
}
