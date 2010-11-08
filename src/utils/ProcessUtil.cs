using System;
using System.ServiceProcess;
using Common.Logging;

namespace NServiceBus.Utils
{
    /// <summary>
    /// Utility class for changing a windows service's status.
    /// </summary>
    public static class ProcessUtil
    {
        /// <summary>
        /// Checks the status of the given controller, and if it isn't the requested state,
        /// performs the given action, and checks the state again.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="status"></param>
        /// <param name="changeStatus"></param>
        public static void ChangeServiceStatus(ServiceController controller, ServiceControllerStatus status, Action changeStatus)
        {
            if (controller.Status == status)
            {
                Logger.Debug(controller.ServiceName + " status is good: " + Enum.GetName(typeof(ServiceControllerStatus), status));
                return;
            }

            Logger.Debug(controller.ServiceName + " status is NOT " + Enum.GetName(typeof(ServiceControllerStatus), status) + ". Changing status...");

            changeStatus();

            var timeout = TimeSpan.FromSeconds(3);
            controller.WaitForStatus(status, timeout);
            if (controller.Status == status)
                Logger.Debug(controller.ServiceName + " status changed successfully.");
            else
                throw new InvalidOperationException("Unable to change " + controller.ServiceName + " status to " + Enum.GetName(typeof(ServiceControllerStatus), status));
        }

        private static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Utils");
    }
}
