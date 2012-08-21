namespace Headquarter.Notifications
{
    using System;
    using NServiceBus.Gateway.Notifications;
    using NServiceBus.Unicast;

    public class MessageForwarded:IWantToRunWhenTheBusStarts
    {
        public IMessageNotifier MessageNotifier { get; set; }
 
        void MessageNotifier_MessageForwarded(object sender, MessageReceivedOnChannelArgs e)
        {
            Console.WriteLine(string.Format("Message with id {0} arrived on {1} and was forwarded onto a {2} channel",e.Message.Id,e.FromChannel,e.ToChannel));
        }

        public void Run()
        {
            MessageNotifier.MessageForwarded += MessageNotifier_MessageForwarded;           
        }

    }
}
