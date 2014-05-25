using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using NServiceBus.MessageMutator;
using log4net;

namespace MessageMutators
{
    public class ValidationMessageMutator : IMessageMutator
    {
        private static readonly ILog Logger = LogManager.GetLogger("ValidationMessageMutator");

        public object MutateOutgoing(object message)
        {
            ValidateDataAnnotations(message);
            return message;
        }

        public object MutateIncoming(object message)
        {
            ValidateDataAnnotations(message);
            return message;
        }
        
        private static void ValidateDataAnnotations(Object message)
        {
            var context = new ValidationContext(message, null, null);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(message, context, results, true);

            if (isValid)
            {
                Logger.Info("Validation succeeded for message: " + message.ToString());
                return;
            }

            var errorMessage = new StringBuilder();
            errorMessage.Append(
                string.Format("Validation failed for message {0}, with the following error/s: " + Environment.NewLine,
                              message.ToString()));

            foreach (var validationResult in results)
                errorMessage.Append(validationResult.ErrorMessage + Environment.NewLine);

            Logger.Error(errorMessage.ToString());
            throw new Exception(errorMessage.ToString());
        }
    }
}
