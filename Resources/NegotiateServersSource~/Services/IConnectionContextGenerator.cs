using Netcode.Transports.AzureWebPubSub;

namespace Netcode.Transports.Azure.RealtimeMessaging.WebPubSub.NegotiateServer.Services
{
    internal interface IConnectionContextGenerator
    {
        public Task<ConnectionContext> NextAsync(string roomId, bool isServer);
    }
}