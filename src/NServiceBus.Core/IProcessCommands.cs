namespace NServiceBus
{
    using JetBrains.Annotations;

    /// <summary>
    /// Defines a command handler.
    /// </summary>
    /// <typeparam name="T">The type of command to be handled.</typeparam>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IProcessCommands<T>
    {
        /// <summary>
        /// Handles a command.
        /// </summary>
        /// <param name="message">The command to handle.</param>
        /// <param name="context">The command context</param>
        /// <remarks>
        /// This method will be called when a command arrives on the bus and should contain
        /// the custom logic to execute when the command is received.</remarks>
        void Handle(T message, ICommandContext context);
    }
}