using Azure.Messaging.WebPubSub;

namespace Netcode.Transports.Azure.RealtimeMessaging.WebPubSub.NegotiateServer.Services
{
    public class RoomManager : IRoomManager
    {
        private WebPubSubServiceClient _serviceClient;

        public RoomManager(WebPubSubServiceClient serviceClient)
        {
            _serviceClient = serviceClient;
        }

        public async Task<bool> ExistAsync(string roomId)
        {
            var res = await _serviceClient.UserExistsAsync(roomId);
            return res.Value;
        }
    }
}