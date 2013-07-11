namespace NServiceBus.Transports.ActiveMQ
{
    using System;

    public class MessageTypeInterpreter : IMessageTypeInterpreter
    {
        public string GetAssemblyQualifiedName(string nmsType)
        {
            if (string.IsNullOrEmpty(nmsType))
            {
                return string.Empty;
            }

            var type = Type.GetType(nmsType);
            if (type != null)
            {
                return type.AssemblyQualifiedName;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(nmsType);
                if (type != null)
                {
                    return type.AssemblyQualifiedName;
                }
            }

            return nmsType;
        }
    }
}