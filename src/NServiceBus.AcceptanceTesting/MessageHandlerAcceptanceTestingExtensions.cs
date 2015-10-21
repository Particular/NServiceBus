namespace NServiceBus.AcceptanceTesting
{
    public static class MessageHandlerAcceptanceTestingExtensions
    {
        public static T GetScenarioContext<T>(this IMessageHandlerContext handlerContext) where T : ScenarioContext
        {
            return (T)handlerContext.Extensions.Get<ScenarioContext>();
        }
    }
}