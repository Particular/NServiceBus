using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus
{
    public abstract class BaseMessageHandler<T> : IMessageHandler<T> where T : IMessage
    {
        public abstract void Handle(T message);

        private IBus bus;
        public IBus Bus { get { return this.bus; } set { this.bus = value; } }
    }
}
