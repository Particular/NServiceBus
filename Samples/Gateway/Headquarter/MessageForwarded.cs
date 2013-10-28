namespace Headquarter
{
    using System;
    using NServiceBus;
    using NServiceBus.Gateway.Notifications;

    public class MessageForwarded : IWantToRunWhenBusStartsAndStops
    {
        public IMessageNotifier MessageNotifier { get; set; }

        private void MessageNotifier_MessageForwarded(object sender, MessageReceivedOnChannelArgs e)
        {
            Console.WriteLine("Message with id {0} arrived on {1} and was forwarded onto a {2} channel", e.Message.Id,
                e.FromChannel, e.ToChannel);
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
