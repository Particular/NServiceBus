
namespace NServiceBus
{
    public abstract class FilteredHandler<T> : IMessageHandler<IMessage>
    {
        public void Handle(IMessage message)
        {
            if (message is T)
                this.Handle((T)message);
        }

        public abstract void Handle(T message);
    }
}
