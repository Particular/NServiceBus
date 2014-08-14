namespace NServiceBus.Settings
{
    using System;

    /// <summary>
    /// Settings related to message serialization
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "6.0")]
    public class SerializationSettings
    {

        /// <summary>
        /// Tells the framework to always wrap out going messages as if there was multiple messages being sent
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "6.0", Message = "In version 5 multi-message sends was removed. So Wrapping messages is no longer required. If you are communicating with version 3 ensure you are on the latest 3.3.x.")]
        public SerializationSettings WrapSingleMessages()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tells the framework to not wrap out going messages as if there was multiple messages being sent
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "6.0", Message = "In version 5 multi-message sends was removed. So Wrapping messages is no longer required. If you are communicating with version 3 ensure you are on the latest 3.3.x.")]
        public SerializationSettings DontWrapSingleMessages()
        {
            throw new NotImplementedException();
        }
    }
}