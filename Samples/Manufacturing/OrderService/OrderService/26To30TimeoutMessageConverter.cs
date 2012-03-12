namespace OrderService
{
    using NServiceBus;
    using NServiceBus.Config;
    using NServiceBus.MessageMutator;
    using NServiceBus.Saga;

    //public class TimeoutMessageConverter:IMutateIncomingMessages,INeedInitialization

    //{
    //    public IBus Bus { get; set; }

    //    public object MutateIncoming(object message)
    //    {
    //        var m = message as TimeoutMessage;
    //        if (m != null)
    //        {
    //            Bus.CurrentMessageContext.Headers[Headers.SagaId] = m.SagaId.ToString();
    //        }
    //        return message;

    //    }

    //    public void Init()
    //    {
    //        Configure.Instance.Configurer.ConfigureComponent<TimeoutMessageConverter>(DependencyLifecycle.SingleInstance);
    //    }
    //}
}