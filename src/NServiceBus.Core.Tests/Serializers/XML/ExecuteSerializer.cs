namespace NServiceBus.Serializers.XML.Test
{
    using System;
    using System.IO;

    public class ExecuteSerializer
    {
        public static T ForMessage<T>(Action<T> a) where T : class,new()
        {
            var msg = new T();
            a(msg);

            return ForMessage<T>(msg);
        }

        public static T ForMessage<T>(object message)
        {
            using (var stream = new MemoryStream())
            {
                SerializerFactory.Create<T>().Serialize(new[] { message }, stream);
                stream.Position = 0;
              
                var msgArray = SerializerFactory.Create<T>().Deserialize(stream, new[]{message.GetType()});
                return (T)msgArray[0];

            }
        }

    }
}