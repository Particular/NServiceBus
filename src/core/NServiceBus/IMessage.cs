using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace NServiceBus
{
    public interface IMessage
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class RecoverableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TimeToBeReceivedAttribute : Attribute
    {
        /// <summary>
        /// Sets the time to be received to be unlimited.
        /// </summary>
        public TimeToBeReceivedAttribute() { }

        public TimeToBeReceivedAttribute(string timeSpan)
        {
            this.timeToBeReceived = TimeSpan.Parse(timeSpan);
        }

        private TimeSpan timeToBeReceived = TimeSpan.MaxValue;
        public TimeSpan TimeToBeReceived
        {
            get { return timeToBeReceived; }
        }
    }
}
