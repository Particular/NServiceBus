using System;
using System.ServiceProcess;
using NServiceBus.Logging;
using System.ComponentModel;

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

            try
            {
                changeStatus();
            }
            catch (Win32Exception exception)
            {
                ThrowUnableToChangeStatus(controller.ServiceName, status, exception);
            }
            catch (InvalidOperationException exception)
            {
                ThrowUnableToChangeStatus(controller.ServiceName, status, exception);
            }

            var timeout = TimeSpan.FromSeconds(3);
            controller.WaitForStatus(status, timeout);
            if (controller.Status == status)
                Logger.Debug(controller.ServiceName + " status changed successfully.");
            else
                ThrowUnableToChangeStatus(controller.ServiceName, status);
        }

        private static void ThrowUnableToChangeStatus(string serviceName, ServiceControllerStatus status)
        {
            ThrowUnableToChangeStatus(serviceName, status, null);
        }

        private static void ThrowUnableToChangeStatus(string serviceName, ServiceControllerStatus status, Exception exception)
        {
            string message = "Unable to change " + serviceName + " status to " + Enum.GetName(typeof(ServiceControllerStatus), status);

            if (exception == null)
            {
                throw new InvalidOperationException(message);
            }

            throw new InvalidOperationException(message, exception);
        }

        private static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Utils");
    }
}