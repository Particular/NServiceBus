
using System;
using ObjectBuilder;

namespace NServiceBus.Serializers
{
    public class Configure : IWith
    {
        public static IWith XmlSerializer
        {
            get
            {
                Configure c = new Configure();
                c.xml = true;

                return c;
            }
        }

        public static IWith BinarySerializer
        {
            get
            {
                Configure c = new Configure();
                c.binary = true;

                return c;
            }
        }

        public void With(IBuilder builder)
        {
            if ((xml && binary) || (!xml && !binary))
                throw new InvalidOperationException("Must define either XML or Binary, not both.");

            if (this.xml)
                builder.ConfigureComponent(typeof(XML.MessageSerializer), ComponentCallModelEnum.Singleton);

            if (this.binary)
                builder.ConfigureComponent(typeof(Binary.MessageSerializer), ComponentCallModelEnum.Singleton);
        }

        private Configure()
        {
            
        }

        private bool xml;
        private bool binary;
    }

    public interface IWith
    {
        void With(IBuilder builder);
    }
}
