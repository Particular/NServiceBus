namespace NServiceBus.Notifications
{
    using System.Security.Principal;
    using Config;
    using Installation;
    using Installation.Environments;

    public class NotifierQueueInstaller:INeedToInstallSomething<Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            if (ConfigureNotifications.NotificationsDisabled)
                return;

            Utils.MsmqUtilities.CreateQueueIfNecessary(BusExtensions.NotificationAddess,identity.Name);
        }
    }
}