namespace NServiceBus.Recoverability;

using System.Net.Http;

/// <summary>
/// TODO
/// </summary>
public static class HttpResponseExtensions
{
    /// <summary>
    /// TODO
    /// </summary>
    public static void EnsureSuccessStatusCodeWithContext(this HttpResponseMessage response)
    {
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            e.Data.Add("NServiceBus.HttpError.HttpResponseHeaders", response.Headers);
            throw;
        }
    }
}