namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public interface ISomeInterface
    {
    }
    public interface ISomeInterface2
    {
    }
    public interface ISomeInterface3
    {
    }

    public class MessageB
    {
    }

    public class MessageA
    {

    }
    public class MessageTypes
    {
        public static IEnumerable<MessageType> MessageA = new[] { new MessageType(typeof(MessageA).FullName, new Version(1, 0, 0, 0)) };
        public static IEnumerable<MessageType> MessageAv2 = new[] { new MessageType(typeof(MessageA).FullName,new Version(2,0,0,0)) };
        public static IEnumerable<MessageType> MessageAv11 = new[] { new MessageType(typeof(MessageA).FullName, new Version(1, 1, 0, 0)) };

        public static IEnumerable<MessageType> MessageB = new[] { new MessageType(typeof(MessageB)) };
    
        public static IEnumerable<MessageType> All = new[] { new MessageType(typeof(MessageA)), new MessageType(typeof(MessageB)) };
    }

    public class TestClients
    {
        public static Address ClientA = Address.Parse("ClientA");
        public static Address ClientB = Address.Parse("ClientB");
        public static Address ClientC = Address.Parse("ClientC");
    }
    
}