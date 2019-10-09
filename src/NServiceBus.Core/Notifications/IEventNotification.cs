namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    interface IEventNotification<TEvent>
    {
        void Subscribe(Func<TEvent, Task> subscription);
    }
}