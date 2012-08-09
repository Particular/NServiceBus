namespace NServiceBus
{
    using System;
    using Config;
    using Notifications;

    public static class BusExtensions
    {
        public static void SendEmail(this IBus bus, MailMessage message)
        {
            if (ConfigureNotifications.NotificationsDisabled)
                throw new InvalidOperationException("Send email is not supported if notifications is disabled. Please remove Configure.DisableNotifications() from your config.");
            
            bus.Send(NotificationAddess, new SendEmail
                                             {
                                                 Message = message
                                             });
        }
   
        public static Address NotificationAddess = Address.Local.SubScope("Notifications");
    }
}