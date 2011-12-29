using System.Linq;
using NServiceBus.MessageMutator;

namespace MessageMutators
{
    public class MultiplierMutator : IMessageMutator
    {
        public object MutateOutgoing(object message)
        {
            var doubles = message.GetType().GetProperties().Where(p => typeof(double).IsAssignableFrom(p.PropertyType));

            foreach (var dbl in doubles)
            {
                var dblValue = (double)dbl.GetValue(message, null);
                dbl.SetValue(message, dblValue * 5, null);
            }

            return message;
        }

        public object MutateIncoming(object message)
        {
            return message;
        }
    }
}
