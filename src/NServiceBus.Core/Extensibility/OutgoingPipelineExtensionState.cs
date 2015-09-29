namespace NServiceBus.Extensibility
{
    using System;

    /// <summary>
    /// Marks a given piece of state as an extension that needs to be handled before the message body is serialized.
    /// </summary>
    public abstract class OutgoingPipelineExtensionState
    {
        bool handled;

        internal void MarkAsHandled()
        {
            handled = true;
        }

        /// <summary>
        /// Returns the error message when the extnesion is not handled (possibly the corresponding feature has been disabled).
        /// </summary>
        protected abstract string GenerateErrorMessageWhenNotHandled();

        internal void ValidateHandled()
        {
            if (!handled)
            {
                throw new Exception(GenerateErrorMessageWhenNotHandled());
            }
        }
    }
}