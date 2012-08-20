namespace Headquarter.Notifications
{
    using System;
    using NServiceBus;
    using NServiceBus.Gateway.Notifications;

    public class MessageForwarded:IWantToRunWhenBusStartsAndStops
    {
        public IMessageNotifier MessageNotifier { get; set; }
 
        void MessageNotifier_MessageForwarded(object sender, MessageReceivedOnChannelArgs e)
        {
            Console.WriteLine(string.Format("Message with id {0} arrived on channel {1} and was forwarded onto {2}",e.Message.Id,e.FromChannel,e.ToChannel));
        }


        public void Start()
        {
            MessageNotifier.MessageForwarded += MessageNotifier_MessageForwarded;
        }

        public void Stop()
        {
            MessageNotifier.MessageForwarded -= MessageNotifier_MessageForwarded;
        }
    }
}
