using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus
{
    public static class ExtensionMethods
    {
        public static void Add<T>(this System.Collections.Generic.IList<T> list, Action<T> constructor) where T : IMessage
        {
            list.Add(MessageCreator.CreateInstance<T>(constructor));
        }

        public static IMessageCreator MessageCreator { get; set; }
    }
}
