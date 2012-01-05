using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Messages;
using NServiceBus.MessageMutator;
using log4net;

namespace MessageMutators
{
    public class ValidationMessageMutator : IMessageMutator
    {
        private static readonly ILog Logger = LogManager.GetLogger("ValidationMessageMutator");

        public object MutateOutgoing(object message)
        {
            var createProductCommand = message as CreateProductCommand;
            if (createProductCommand == null)
                return message;

            ValidateProductCommand(createProductCommand);
            
            return createProductCommand;
        }

        

        public object MutateIncoming(object message)
        {
            var createProductCommand = message as CreateProductCommand;
            if (createProductCommand == null)
                return message;

            ValidateProductCommand(createProductCommand);

            return createProductCommand;
        }
        
        private static void ValidateProductCommand(CreateProductCommand createProductCommand)
        {
            var context = new ValidationContext(createProductCommand, null, null);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(createProductCommand, context, results, true);

            if (isValid)
            {
                Logger.Info("Validation succeeded for message: " + createProductCommand.ToString());
                return;
            }

            var errorMessage = new StringBuilder();
            errorMessage.Append(
                string.Format("Validation failed for message {0}, with the following error/s: " + Environment.NewLine,
                              createProductCommand.ToString()));

            foreach (var validationResult in results)
                errorMessage.Append(validationResult.ErrorMessage + Environment.NewLine);

            Logger.Error(errorMessage.ToString());
            throw new Exception(errorMessage.ToString());
        }
    }
}
