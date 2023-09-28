namespace Netcode.Transports.Azure.RealtimeMessaging.WebPubSub.NegotiateServer.Services
{
    public interface IRoomManager
    {
        public Task<bool> ExistAsync(string roomId);
    }
}