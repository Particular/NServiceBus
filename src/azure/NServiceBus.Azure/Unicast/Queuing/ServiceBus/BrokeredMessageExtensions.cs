using System;
using System.Transactions;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    /// <summary>
    /// 
    /// </summary>
    public static class BrokeredMessageExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool SafeComplete(this BrokeredMessage msg)
        {
            try
            {
                msg.Complete();

                return true;
            }
            catch (MessageLockLostException)
            {
                // It's too late to compensate the loss of a message lock. We should just ignore it so that it does not break the receive loop.
            }
            catch (MessagingException)
            {
                // There is nothing we can do as the connection may have been lost, or the underlying queue may have been removed.
                // If Abandon() fails with this exception, the only recourse is to receive another message.
            }
            catch (ObjectDisposedException)
            {
                // There is nothing we can do as the object has already been disposed elsewhere
            }
            catch (TransactionException)
            {
                // 
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool SafeAbandon(this BrokeredMessage msg)
        {
            try
            {
                msg.Abandon();

                return true;
            }
            catch (MessageLockLostException)
            {
                // It's too late to compensate the loss of a message lock. We should just ignore it so that it does not break the receive loop.
            }
            catch (MessagingException)
            {
                // There is nothing we can do as the connection may have been lost, or the underlying queue may have been removed.
                // If Abandon() fails with this exception, the only recourse is to receive another message.
            }
            catch (ObjectDisposedException)
            {
                // There is nothing we can do as the object has already been disposed elsewhere
            }
            catch (TransactionException)
            {
                // 
            }
            return false;
        }
    }
}