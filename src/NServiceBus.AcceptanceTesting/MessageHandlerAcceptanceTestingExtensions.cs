namespace NServiceBus.AcceptanceTesting
{
    using NServiceBus.Extensibility;

    public static class MessageHandlerAcceptanceTestingExtensions
    {
        public static T GetScenarioContext<T>(this IMessageHandlerContext handlerContext) where T : ScenarioContext
        {
            return (T)handlerContext.GetExtensions().Get<ScenarioContext>();
        }
    }
}